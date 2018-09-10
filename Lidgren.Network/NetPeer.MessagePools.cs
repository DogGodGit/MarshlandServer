using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public partial class NetPeer
	{
		private NetQueue<NetOutgoingMessage> m_outgoingMessagesPool;
		private NetQueue<NetIncomingMessage> m_incomingMessagesPool;

		internal int m_storagePoolBytes;

		private readonly int kStartPoolSize = 16;
		private readonly int kNormalStorageSize = 1024 * 2;	// MTU = 1408
		private Object m_lockObj;
		private Stack<byte[]> m_storageNormalPool;
		private List<byte[]> m_storageExtraPool;

		private void InitializePools()
		{
			if (m_configuration.UseMessageRecycling)
			{
				m_outgoingMessagesPool = new NetQueue<NetOutgoingMessage>(4);
				m_incomingMessagesPool = new NetQueue<NetIncomingMessage>(4);

				m_lockObj = new Object();

				m_storageNormalPool = new Stack<byte[]>(kStartPoolSize);
				m_storageExtraPool = new List<byte[]>(kStartPoolSize);
			}
			else
			{
				m_outgoingMessagesPool = null;
				m_incomingMessagesPool = null;

				m_lockObj = null;

				m_storageNormalPool = null;
				m_storageExtraPool = null;
			}
		}

		internal byte[] GetStorage(int minimumCapacityInBytes)
		{
			if (minimumCapacityInBytes < kNormalStorageSize)
				minimumCapacityInBytes = kNormalStorageSize;
			
			if (m_lockObj == null)
				return new byte[minimumCapacityInBytes];

			lock (m_lockObj)
			{
				if (minimumCapacityInBytes > kNormalStorageSize)
				{
					// larger than normal, use list
					int cnt = m_storageExtraPool.Count;
					for (int i = 0; i < cnt; ++i)
					{
						byte[] retval = m_storageExtraPool[i];
					if (retval != null && retval.Length >= minimumCapacityInBytes)
					{
							m_storageExtraPool[i] = null;
						m_storagePoolBytes -= retval.Length;
						return retval;
					}
				}
			}
				else
				{
					// normal size, use stack
					if (m_storageNormalPool.Count > 0)
					{
						byte[] retval = m_storageNormalPool.Pop();
						m_storagePoolBytes -= retval.Length;
						return retval;
					}
				}

			}
			m_statistics.m_bytesAllocated += minimumCapacityInBytes;
			return new byte[minimumCapacityInBytes];
		}

		internal void Recycle(byte[] storage)
		{
			if (m_lockObj == null || storage == null)
				return;

			lock (m_lockObj)
			{
				int len = storage.Length;
				m_storagePoolBytes += len;
				if (len > kNormalStorageSize)
				{
					int cnt = m_storageExtraPool.Count;
				for (int i = 0; i < cnt; i++)
				{
						if (m_storageExtraPool[i] == null)
					{
							m_storageExtraPool[i] = storage;
						return;
					}
				}
					m_storageExtraPool.Add(storage);
				}
				else
				{
					m_storageNormalPool.Push(storage);
				}
			}
		}

		/// <summary>
		/// Creates a new message for sending
		/// </summary>
		public NetOutgoingMessage CreateMessage()
		{
			return CreateMessage(m_configuration.m_defaultOutgoingMessageCapacity);
		}

		/// <summary>
		/// Creates a new message for sending and writes the provided string to it
		/// </summary>
		public NetOutgoingMessage CreateMessage(string content)
		{
			var om = CreateMessage(2 + content.Length); // fair guess
			om.Write(content);
			return om;
		}

		/// <summary>
		/// Creates a new message for sending
		/// </summary>
		/// <param name="initialCapacity">initial capacity in bytes</param>
		public NetOutgoingMessage CreateMessage(int initialCapacity)
		{
			NetOutgoingMessage retval;
			if (m_outgoingMessagesPool == null || !m_outgoingMessagesPool.TryDequeue(out retval))
				retval = new NetOutgoingMessage();

			if (initialCapacity > 0)
				retval.m_data = GetStorage(initialCapacity);

			return retval;
		}

		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, byte[] useStorageData)
		{
			NetIncomingMessage retval;
			if (m_incomingMessagesPool == null || !m_incomingMessagesPool.TryDequeue(out retval))
				retval = new NetIncomingMessage(tp);
			else
				retval.m_incomingMessageType = tp;
			retval.m_data = useStorageData;
			return retval;
		}

		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, int minimumByteSize)
		{
			NetIncomingMessage retval;
			if (m_incomingMessagesPool == null || !m_incomingMessagesPool.TryDequeue(out retval))
				retval = new NetIncomingMessage(tp);
			else
				retval.m_incomingMessageType = tp;
			retval.m_data = GetStorage(minimumByteSize);
			return retval;
		}

		/// <summary>
		/// Recycles a NetIncomingMessage instance for reuse; taking pressure off the garbage collector
		/// </summary>
		public void Recycle(NetIncomingMessage msg)
		{
			if (m_incomingMessagesPool == null)
				return;

			NetException.Assert(m_incomingMessagesPool.Contains(msg) == false, "Recyling already recycled message! Thread race?");

			byte[] storage = msg.m_data;
			msg.m_data = null;
			Recycle(storage);
			msg.Reset();
			m_incomingMessagesPool.Enqueue(msg);
		}

		/// <summary>
		/// Recycles a list of NetIncomingMessage instances for reuse; taking pressure off the garbage collector
		/// </summary>
		public void Recycle(IEnumerable<NetIncomingMessage> toRecycle)
		{
			if (m_incomingMessagesPool == null)
				return;

			// first recycle the storage of each message
			if (m_lockObj != null)
			{
				lock (m_lockObj)
				{
					foreach (var msg in toRecycle)
					{
						var storage = msg.m_data;
						msg.m_data = null;
						m_storagePoolBytes += storage.Length;
						Recycle(storage);
						msg.Reset();
					}
				}
			}

			// then recycle the message objects
			m_incomingMessagesPool.Enqueue(toRecycle);
		}

		internal void Recycle(NetOutgoingMessage msg)
		{
			if (m_outgoingMessagesPool == null)
				return;

			NetException.Assert(m_outgoingMessagesPool.Contains(msg) == false, "Recyling already recycled message! Thread race?");
			
			byte[] storage = msg.m_data;
			msg.m_data = null;
			
			// message fragments cannot be recycled
			// TODO: find a way to recycle large message after all fragments has been acknowledged; or? possibly better just to garbage collect them
			if (msg.m_fragmentGroup == 0)
				Recycle(storage);
	
			msg.Reset();
			m_outgoingMessagesPool.Enqueue(msg);
		}

		/// <summary>
		/// Creates an incoming message with the required capacity for releasing to the application
		/// </summary>
		internal NetIncomingMessage CreateIncomingMessage(NetIncomingMessageType tp, string text)
		{
			NetIncomingMessage retval;
			if (string.IsNullOrEmpty(text))
			{
				retval = CreateIncomingMessage(tp, 1);
				retval.Write(string.Empty);
				return retval;
			}

			int numBytes = System.Text.Encoding.UTF8.GetByteCount(text);
			retval = CreateIncomingMessage(tp, numBytes + (numBytes > 127 ? 2 : 1));
			retval.Write(text);

			return retval;
		}
	}
}
