using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace MainServer
{
    public delegate void SQLSuccessDelegate();
    public delegate void SQLFailureDelegate();

    public class Database
    {
        private Object objLock = new Object();

        public const int LONG_QUERY_THRESHOLD = 100;
        public const int VERY_LONG_QUERY_THRESHOLD = 2000;

        public static bool debug_database;
        public string conString;
        public Queue<string> m_syncStatements = new Queue<string>();
		public Queue<MySqlParameter[]> m_syncStatementParams = new Queue<MySqlParameter[]>();
		public Thread workerThread;
        public bool m_exitThread;
        public bool m_finishedThread;

        public Queue<SQLSuccessDelegate> m_successCallbacks = new Queue<SQLSuccessDelegate>();
        public Queue<SQLFailureDelegate> m_failureCallbacks = new Queue<SQLFailureDelegate>();

        public Database(string connectionString)
        {
            conString = connectionString;
        }

        public void SpawnThread()
        {
            m_exitThread = false;
            m_finishedThread = false;
            workerThread = new Thread(() => runSyncCommandsLoop(conString));
			workerThread.Name = "MySQLSyncThread";
            //      workerThread.Priority = ThreadPriority.BelowNormal;
            workerThread.Start();
        }

        public void DespawnThread()
        {
            m_exitThread = true;
        }

        void runSyncCommandsLoop(string connectionString)
        {
            while (true)
            {
                string sqlstr = String.Empty;
				MySqlParameter[] sqlParams = null;
                SQLSuccessDelegate successDelegate = delegate {};
                SQLFailureDelegate failureDelegate = delegate {};
                int statementCount;
                lock (objLock)
                {
                    statementCount = m_syncStatements.Count;
                    if (m_syncStatements.Count > 0)
                    {
                        sqlstr = m_syncStatements.Dequeue();
						sqlParams = m_syncStatementParams.Dequeue();
						successDelegate = m_successCallbacks.Dequeue();
                        failureDelegate = m_failureCallbacks.Dequeue();
                    }
                }
                if (sqlstr.Length > 0)
                {
                    DateTime start = DateTime.Now;
                    try
                    {
                        MySqlConnection connection = new MySqlConnection(connectionString);
                        try
                        {
                            connection.Open();
                            MySqlCommand command = connection.CreateCommand();
                            command.CommandText = sqlstr;
							if (sqlParams != null)
							{
								command.Parameters.AddRange(sqlParams);
							}
                            int res = command.ExecuteNonQuery();
                            if (res == 0)
                            {
                                if (!sqlstr.StartsWith("delete from pvp_recent_kills"))
                                {
                                    Program.DisplayDelayed("Sync Command run : " + sqlstr + " no rows affected");
                                }
                            }

                            //successDelegate();
                        }
                        catch (Exception e)
                        {
                            failureDelegate();
                            Program.LogDatabaseException("NonQuereyException: " + sqlstr + " \r\n" + e.GetType() + " " + e.Message + " " + e.StackTrace);
                        }
                        finally
                        {
                            connection.Close();
                            connection = null;
                        }

                        successDelegate();

                        double timetaken = (DateTime.Now - start).TotalMilliseconds;
                        if (debug_database || timetaken > VERY_LONG_QUERY_THRESHOLD)
                        {
                            Program.DisplayDelayed("Sync Command " + timetaken + " : " + sqlstr);
                        }
                    }
                    catch (Exception e1)
                    {
                        Program.LogDatabaseException("GeneralException: " + sqlstr + " \r\n" + e1.GetType() + " " + e1.Message + " " + e1.StackTrace);
                    }
                }

                Thread.Sleep(1);
                if (m_exitThread)
                {
                    lock (objLock)
                    {
                        statementCount = m_syncStatements.Count;
                    }
                    if (statementCount == 0)
                    {
                        break;
                    }
                }
            }

            m_finishedThread = true;
        }

        public void runCommandSync(string sqlstr, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
        {
            if (sqlstr.Length > 0)
            {
                if (debug_database)
                    Program.Display("Enqueuing " + sqlstr);
                lock (objLock)
                {
                    m_syncStatements.Enqueue(sqlstr);
					m_syncStatementParams.Enqueue(null);


					if (successDelegate == null)
                    {
                        successDelegate = delegate { };
                    }

                    if (failureDelegate == null)
                    {
                        failureDelegate = delegate { };
                    }

                    m_successCallbacks.Enqueue(successDelegate);
                    m_failureCallbacks.Enqueue(failureDelegate);
                }
            }
            else
            {
                Program.Display("trying to add zero length command string " + Environment.StackTrace);
            }
        }

		public void runCommandSyncWithParams(string sqlstr, MySqlParameter[] sqlParams, MainServer.SQLSuccessDelegate successDelegate = null, MainServer.SQLFailureDelegate failureDelegate = null)
		{
			if (sqlstr.Length > 0)
			{
				if (debug_database)
					Program.Display("Enqueuing " + sqlstr);
				lock (objLock)
				{
					m_syncStatements.Enqueue(sqlstr);
					m_syncStatementParams.Enqueue(sqlParams);

					if (successDelegate == null)
					{
						successDelegate = delegate { };
					}

					if (failureDelegate == null)
					{
						failureDelegate = delegate { };
					}

					m_successCallbacks.Enqueue(successDelegate);
					m_failureCallbacks.Enqueue(failureDelegate);
				}
			}
			else
			{
				Program.Display("trying to add zero length command string " + Environment.StackTrace);
			}
		}

		public bool runCommand(string sqlstr)
        {
            return runCommand(sqlstr, false);
        }
        public bool runCommand(string sqlstr, bool background)
        {
            DateTime start = DateTime.Now;

            MySqlConnection connection = new MySqlConnection(conString);
            bool ret = true;

            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = sqlstr;
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Program.LogDatabaseException(sqlstr + " \r\n" + e.GetType() + " " + e.Message);
            }
            finally
            {
                connection.Close();
                connection = null;
            }
            double timetaken = (DateTime.Now - start).TotalMilliseconds;
            if (background)
            {
                if (debug_database || timetaken > VERY_LONG_QUERY_THRESHOLD)
                {
                    Program.Display("Background Command " + timetaken + " : " + sqlstr);
                }
            }
            else
            {
                if (debug_database || timetaken > LONG_QUERY_THRESHOLD)
                {
                    Program.Display("Command " + timetaken + " : " + sqlstr);
                }
            }

            return ret;
        }

		public bool runCommandWithParams(string sqlstr, MySqlParameter[] parameters, bool background = false)
		{
			DateTime start = DateTime.Now;

			MySqlConnection connection = new MySqlConnection(conString);
			bool ret = true;

			try
			{
				connection.Open();
				MySqlCommand command = connection.CreateCommand();
				command.CommandText = sqlstr;
				if (parameters != null)
				{
					command.Parameters.AddRange(parameters);
				}
				command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				Program.LogDatabaseException(sqlstr + " \r\n" + e.GetType() + " " + e.Message);
			}
			finally
			{
				connection.Close();
				connection = null;
			}
			double timetaken = (DateTime.Now - start).TotalMilliseconds;
			if (background)
			{
				if (debug_database || timetaken > VERY_LONG_QUERY_THRESHOLD)
				{
					Program.Display("Background Command " + timetaken + " : " + sqlstr);
				}
			}
			else
			{
				if (debug_database || timetaken > LONG_QUERY_THRESHOLD)
				{
					Program.Display("Command " + timetaken + " : " + sqlstr);
				}
			}

			return ret;
		}

		public bool runCommandsInTransaction(List<string> sqlstr)
        {
            MySqlConnection connection = new MySqlConnection(conString);
            connection.Open();

            bool ret = true;
            MySqlCommand command = connection.CreateCommand();
            MySqlTransaction myTrans;
            myTrans = connection.BeginTransaction();
            command.Transaction = myTrans;
            int i = 0;
            try
            {
                for (i = 0; i < sqlstr.Count; i++)
                {
                    command.CommandText = sqlstr[i];
                    int res = command.ExecuteNonQuery();
                }
                myTrans.Commit();
            }
            catch (Exception e)
            {
                myTrans.Rollback();
                Program.DisplayDelayed("failed to run transactions \r\n" + sqlstr[i] + "\r\n" + e.Message + "\r\n" + e.StackTrace);
            }
            finally
            {
                connection.Close();
                connection = null;
            }
            return ret;
        }
    }

    public class SqlQuery
    {
        private MySqlConnection m_Connection;
        private MySqlDataReader m_Reader;

        public SqlQuery(Database db)
        {
            m_Connection = new MySqlConnection(db.conString);
        }

        public void ExecuteCommand(string queryString)
        {
            DateTime start = DateTime.Now;
            if (m_Connection.State == ConnectionState.Open)
            {
                m_Connection.Close(); // check just to make sure the connection is closed before we try to open 
            }
            m_Connection.Open();

            m_Reader = null;

            try
            {
                MySqlCommand command = m_Connection.CreateCommand();
                command.CommandText = queryString;
                m_Reader = command.ExecuteReader();
                command.Dispose();
            }
            catch (Exception e)
            {
                Program.LogDatabaseException(queryString + Environment.NewLine + e.GetType() + " " + e.Message + " " + e.StackTrace);
            }

            double timetaken = (DateTime.Now - start).TotalMilliseconds;
            if (Database.debug_database || timetaken > 100)
            {
                Program.Display("Query " + timetaken + ": " + queryString);
            }
        }

        /// <summary>
        /// This should be called after an ExecuteCommand so the query doesn't get rid of the connection but can carry out another command
        /// </summary>
        public void CleanUpAfterExecute()
        {
            if (m_Connection != null)
                m_Connection.Close();
            if (m_Reader != null)
                m_Reader.Close();
        }


        public SqlQuery(Database db, string queryString)
        {
            DateTime start = DateTime.Now;

            m_Connection = new MySqlConnection(db.conString);
            m_Connection.Open();
            m_Reader = null;

            try
            {
                MySqlCommand command = m_Connection.CreateCommand();
                command.CommandText = queryString;
                m_Reader = command.ExecuteReader();
                command.Dispose();
            }
            catch (Exception e)
            {
                Program.LogDatabaseException(queryString + " \r\n" + e.GetType() + " " + e.Message + " " + e.StackTrace);
            }
            double timetaken = (DateTime.Now - start).TotalMilliseconds;
            if (Database.debug_database || timetaken > 100)
            {
                Program.Display("Query " + timetaken + " : " + queryString);
            }
        }

		public SqlQuery(Database db, string queryString, MySqlParameter[] parameters)
		{
			DateTime start = DateTime.Now;

			m_Connection = new MySqlConnection(db.conString);
			m_Connection.Open();
			m_Reader = null;

			try
			{
				MySqlCommand command = m_Connection.CreateCommand();
				command.CommandText = queryString;
				if (parameters != null)
				{
					command.Parameters.AddRange(parameters);
				}
				m_Reader = command.ExecuteReader();
				command.Dispose();
			}
			catch (Exception e)
			{
				Program.LogDatabaseException(queryString + " \r\n" + e.GetType() + " " + e.Message + " " + e.StackTrace);
			}
			double timetaken = (DateTime.Now - start).TotalMilliseconds;
			if (Database.debug_database || timetaken > 100)
			{
				Program.Display("Query " + timetaken + " : " + queryString);
			}
		}

		public SqlQuery(Database db, string queryString, bool background, bool in_rethrowExceptions = false)
        {
            DateTime start = DateTime.Now;

            try
            {
            m_Connection = new MySqlConnection(db.conString);
            m_Connection.Open();
            m_Reader = null;

                MySqlCommand command = m_Connection.CreateCommand();
                command.CommandText = queryString;
                m_Reader = command.ExecuteReader();
                command.Dispose();
            }
            catch (Exception e)
            {
                Program.LogDatabaseException(queryString + " \r\n" + e.GetType() + " " + e.Message + " " + e.StackTrace);
                
                if (in_rethrowExceptions)
                  throw e;
            }
            double timetaken = (DateTime.Now - start).TotalMilliseconds;
            if (background)
            {
                if (Database.debug_database || timetaken > Database.VERY_LONG_QUERY_THRESHOLD)
                {
                    Program.Display("Background Query " + timetaken + " : " + queryString);
                }
            }
            else
            {
                if (Database.debug_database || timetaken > Database.LONG_QUERY_THRESHOLD)
                {
                    Program.Display("Query " + timetaken + " : " + queryString);
                }
            }
        }

		public SqlQuery(Database db, string queryString, MySqlParameter[] parameters, bool background, bool in_rethrowExceptions = false)
		{
			DateTime start = DateTime.Now;

			try
			{
				m_Connection = new MySqlConnection(db.conString);
				m_Connection.Open();
				m_Reader = null;

				MySqlCommand command = m_Connection.CreateCommand();
				command.CommandText = queryString;
				if (parameters != null)
				{
					command.Parameters.AddRange(parameters);
				}
				m_Reader = command.ExecuteReader();
				command.Dispose();
			}
			catch (Exception e)
			{
				Program.LogDatabaseException(queryString + " \r\n" + e.GetType() + " " + e.Message + " " + e.StackTrace);

				if (in_rethrowExceptions)
					throw e;
			}
			double timetaken = (DateTime.Now - start).TotalMilliseconds;
			if (background)
			{
				if (Database.debug_database || timetaken > Database.VERY_LONG_QUERY_THRESHOLD)
				{
					Program.Display("Background Query " + timetaken + " : " + queryString);
				}
			}
			else
			{
				if (Database.debug_database || timetaken > Database.LONG_QUERY_THRESHOLD)
				{
					Program.Display("Query " + timetaken + " : " + queryString);
				}
			}
		}

		public void Close()
        {
            if (m_Reader != null)
            {
                m_Reader.Close();
                m_Reader = null;
            }
            if (m_Connection != null)
            {
                m_Connection.Close();
                m_Connection.Dispose();
                m_Connection = null;
            }

        }

        ~SqlQuery()
        {
            Close();
        }
        public bool HasRows
        {
            get
            {
                if (m_Reader != null && m_Reader.HasRows)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool Read()
        {
            if (m_Reader != null)
                return m_Reader.Read();
            else
                return false;
        }
        public int GetInt32(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetInt32(value);
            else
                throw new Exception("no Valid Reader");
        }
        public Int64 GetInt64(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetInt64(value);
            else
                throw new Exception("no Valid Reader");
        }
        public uint GetUInt32(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetUInt32(value);
            else
                throw new Exception("no Valid Reader");
        }
        public UInt64 GetUInt64(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetUInt64(value);
            else
                throw new Exception("no Valid Reader");
        }
        public bool GetBoolean(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetBoolean(value);
            else
                throw new Exception("no Valid Reader");
        }

        public string GetString(string value)
        {
            if (m_Reader != null)
            {
                int ordinal = m_Reader.GetOrdinal(value);
                return !m_Reader.IsDBNull(ordinal) ? m_Reader.GetString(value) : String.Empty;
            }
            else
                throw new Exception("no Valid Reader");
        }
        public float GetFloat(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetFloat(value);
            else
                throw new Exception("no Valid Reader");
        }

        public double GetDouble(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetDouble(value);
            else
                throw new Exception("no Valid Reader");
        }
        public DateTime GetDateTime(string value)
        {
            if (m_Reader != null)
                return m_Reader.GetDateTime(value);
            else
                throw new Exception("no Valid Reader");
        }
        public bool isNull(string value)
        {
            int index = m_Reader.GetOrdinal(value);
            return m_Reader.IsDBNull(index);
        }
    }

}