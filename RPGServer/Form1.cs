using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using MainServer.DailyLoginReward;
using SamplesCommon;
using MainServer.partitioning;
using XnaGeometry;
using MainServer.player_offers;
using Lidgren.Network;
using MainServer.Collisions;
using System.IO;
using System.Text;

namespace MainServer
{
    public partial class Form1 : Form
    {
        public int m_drawnZoneIndex = 0;
        public int m_flashtoggle = 0;
        public int m_zoomLevel = 0;
        public float m_mapOffsetX = 0;
        public float m_mapOffsetY = 0;
        public int m_oldMouseX = 0;
        public int m_oldMouseY = 0;
        public bool m_mouseDown = false;
        public int m_currentTab = 0;
        public Image m_currentMap;
        public string m_currentMapFile;

        public DateTime AHResetTime = DateTime.Now;
        public bool AHResetTimeSet = false;
        public int AuctionHouseStatus { set; get; }
        public bool shutdown;
        public bool DebugItems { get { return this.checkBoxDebug.Checked; } }

        internal static bool s_OfflineEmailNotificationsEnabled = true;

        //Seasonal tweaker files
        public List<String> seasons = new List<string>();
        public List<String> winterFilenames = new List<string>();
        public List<String> summerFilenames = new List<string>();
        public List<String> fallFilenames = new List<string>();
        public List<String> springFilenames = new List<string>();
        public enum SEASON
        {
            Winter,
            Summer,
            Fall,
            Spring
        }
        public Dictionary<string, List<string>> seasonDictionary = new Dictionary<string, List<string>>();

        public static Timer refreshPlayersTimer;

		private StringBuilder sbLogs = new StringBuilder();
		private static Object lockObjLogs = new Object();
		private StringBuilder sbBuffer = new StringBuilder();
		private int logEntryCount = 0;
		private const int kMaxEntryToKeep = 1000;

		public Form1()
        {
            InitializeComponent();

            AuctionHouseStatus = -1;

            //start in focus so we auto scroll
            AddFocusToRichTextBox();
            populateSeasons();

            //Set the combobox initial value to the season saved in app.config

            if (ConfigurationManager.AppSettings["currentSeason"] != null)
            {
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    if (comboBox1.Items[i].ToString() == ConfigurationManager.AppSettings["currentSeason"])
                    {
                        comboBox1.SelectedIndex = i;
                    }
                }
            }
            else
            {
                MessageBox.Show("Key: 'currentSeason' missing from exe.config, please add key.","Missing Key",MessageBoxButtons.OK);
            }
           
        }

        public class ComboboxItem
        {
            public string Text { get; set; }
            public object Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (Program.SettingsWindow == null || Program.SettingsWindow.IsDisposed)
                Program.SettingsWindow = new NetPeerSettingsWindow("Client settings", Program.Server);
            if (Program.SettingsWindow.Visible)
                Program.SettingsWindow.Hide();
            else
                Program.SettingsWindow.Show();
        }

        public void loadMaps()
        {
            /* mapViewControl.init();
             mapMeshes = new CModelASE[Program.processor.m_zones.Length];
             for (int i = 0; i < Program.processor.m_zones.Length; i++)
             {
                 mapMeshes[i] = new CModelASE(mapViewControl.GraphicsDevice);
                 mapMeshes[i].loadMesh("levels\\" + Program.processor.m_zones[i]);
                 cbZone.Items.Add(Program.processor.m_zones[i].m_zone_name);
            
             }
             cbZone.Enabled = true;
         */
        }

        /// <summary>
        /// Reads from a text file on the server to build the seasonal profiles that can
        /// be selected.
        /// </summary>
        public void populateSeasons()
        {
            if (File.Exists("seasonTweaks.txt"))
            {
                string[] lines = File.ReadAllLines("seasonTweaks.txt");

                foreach (string line in lines)
                {
                    //Split the line by commas
                    string[] col = line.Split(new char[] { ',' });

                    //Add a new list to the dictionary, keyed by season name
                    seasonDictionary.Add(col[0], new List<string>());

                    //Add each of the season keywords to the appropriate list
                    for (int i = 1; i < col.Length; i++)
                    {
                        seasonDictionary[col[0]].Add(col[i]);
                    }

                    //Add the season name to the combo box on the server form
                    ComboboxItem item = new ComboboxItem();
                    item.Text = col[0].ToString();
                    item.Value = col[0];
                    comboBox1.Items.Add(item);
                }
            }
            else
            {
                DialogResult result = MessageBox.Show("File:seasonTweaks.txt is missing, please add seasonal profile.","File Missing", MessageBoxButtons.OK);

                if (result == DialogResult.OK)
                {
                    shutdown = true;
                }
            }
        }


        private void cbZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_drawnZoneIndex = ((ComboBox)sender).SelectedIndex;
            pn2Dmap.Invalidate();
        }


        private PointF convertToScreenCoords(Zone zone, RectangleF destRect, double x, double y, double zoom)
        {
            double newX = (x - zone.m_zoneRect.Left) * destRect.Width / zone.m_zoneRect.Width;
            double newY = destRect.Height * (zone.m_zoneRect.Bottom - y) / zone.m_zoneRect.Height;
            newX = newX + destRect.X;
            newY = newY + destRect.Y;
            return new PointF((float)newX, (float)newY);
        }



        private void pn2Dmap_Paint(object sender, PaintEventArgs e)
        {
            float zoomAmount = (float)Math.Pow(2, m_zoomLevel);
            if (m_drawnZoneIndex == 0)
                return;
            m_flashtoggle++;
            Zone drawingZone = Program.processor.m_zones[m_drawnZoneIndex - 1];
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), e.ClipRectangle);
            float destSize = Math.Min(e.ClipRectangle.Width, e.ClipRectangle.Height) * zoomAmount; ;

            RectangleF destRect = new RectangleF(e.ClipRectangle.Left + (e.ClipRectangle.Width * zoomAmount - destSize) / 2, e.ClipRectangle.Top + (e.ClipRectangle.Height * zoomAmount - destSize) / 2, destSize, destSize);
            destRect.Offset(m_mapOffsetX * zoomAmount, m_mapOffsetY * zoomAmount);

            if (drawingZone.m_mapfilename == "")
            {
                m_currentMap = null;
                m_currentMapFile = string.Empty;
            }
            else
            {
                string filename = drawingZone.m_mapfilename;
                if (!chIngameMap.Checked)
                {
                    //string .png and add _render
                    string extension = Path.GetExtension(filename);
                    filename = Path.ChangeExtension(filename, null);
                    filename += "_render" + extension;
                }

                if (filename != m_currentMapFile)
                {
                    try
                    {
                        m_currentMap = Bitmap.FromFile(ConfigurationManager.AppSettings["CollisionMapPath"] + filename);
                        m_currentMapFile = filename;
                    }
                    catch (Exception exception)
                    {
                        Program.Display(filename + " missing" + exception.Message);
                        m_currentMapFile = string.Empty;
                        m_currentMap = null;
                    }
                }
            }

            if (m_currentMap != null)
            {
                int maxSize = Math.Max(m_currentMap.Width, m_currentMap.Height);

                RectangleF srcRect = new RectangleF(0, 0, m_currentMap.Width, m_currentMap.Height);

                e.Graphics.DrawImage(m_currentMap, destRect, srcRect, GraphicsUnit.Pixel);
            }

            Pen BlackPen = new Pen(Brushes.Black,2.1f);
            
            Pen RedPen = new Pen(Brushes.Red,2.1f);
            Pen GreenPen = new Pen(Brushes.Green,2.1f);
            Pen YellowPen = new Pen(Brushes.Orange,2.1f);
            Pen BluePen = new Pen(Brushes.Blue,2.1f);
            Brush ShadowBrush = new SolidBrush(Color.FromArgb(128, 64, 64, 64));
            Pen WhitePen = new Pen(Brushes.White,2.1f);
            Brush clearbrush=new SolidBrush(Color.FromArgb(100,64,64,64));
            Brush cornflowerbluebrush = new SolidBrush(Color.FromArgb(100, 101, 156, 239));
            Pen purplePen = new Pen(Brushes.Purple);
            Pen magentaPen = new Pen(Brushes.Magenta);
          //  purplePen.Width = 2.1f;

            Pen AreaSelected = new Pen(Brushes.Magenta,2.1f);
        
            Font theFont = new Font("Arial", 8);
            float playWidth = drawingZone.m_zoneRect.Width;
            float playHeight = drawingZone.m_zoneRect.Height;
            float playcentrex = drawingZone.m_zoneCentre.X;
            float playcentrey = drawingZone.m_zoneCentre.Y;
            float scaleAdjustmentx = destRect.Width / playWidth;
            float scaleAdjustmenty = destRect.Height / playHeight;
            //draw partition lines
            ZonePartitionHolder partitions = drawingZone.PartitionHolder;
            Vector2 startPos = partitions.StartLocation;

            Character areaTestCharacter = null;

            if (chShowAIMap.Checked)
            {
                for (int i = 0; i < drawingZone.PathFinder.TheMap.Triangles.Count; i++)
                {
                   // if (i == 164 || i == 166 || i == 167 || i == 168 || i == 202 || i == 204 || i == 208)
                    {
                        ASTriangle triangle = drawingZone.PathFinder.TheMap.Triangles[i];
                        PointF[] points = new PointF[3];
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 vertex = triangle.GetVertices(j);
                            points[j] = convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount);
                        }
                        //drawingZone.
                        e.Graphics.FillPolygon(clearbrush, points);
                        e.Graphics.DrawPolygon(purplePen, points);
                    }
                }
            }
 
            if (chkShowCollisionMap.Checked)
            {
                List<CCollisionObject> allColliders = new List<CCollisionObject>();

                drawingZone.Collison.getAllCollisionObjects(drawingZone.Collison.m_allObjectsRootNode, allColliders);

                List<PointF> vertices = new List<PointF>();

                for (int i = 0; i < allColliders.Count; i++)
                {
                    Vector3 vertex = Vector3.Zero;

                    switch (allColliders[i].m_objectType)
                    {
                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AACYLINDER:
                            CCollision_AACylinder cylinder = (CCollision_AACylinder)allColliders[i];

                            Vector3 center = cylinder.m_bounding_Sphere.m_centre;
                            float radius = (float)cylinder.m_bounding_Circle.m_radius;

                            float segment = (float)(2 * Math.PI / 16);
                            for (int a = 0; a < 16; a++)
                            {
                                float angle = segment * a;
                                float circleX = (float)(center.X + radius * Math.Cos(angle));
                                float circleZ = (float)(center.Z + radius * Math.Sin(angle));

                                vertices.Add(convertToScreenCoords(drawingZone, destRect, circleX, circleZ, zoomAmount));
                            }

                            e.Graphics.FillPolygon(cornflowerbluebrush, vertices.ToArray());
                            e.Graphics.DrawPolygon(magentaPen, vertices.ToArray());

                            vertices.Clear();

                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_AABB:
                            CCollision_AABB aabb = (CCollision_AABB)allColliders[i];

                            vertex.X = aabb.m_aabb.m_min.X;
                            vertex.Z = aabb.m_aabb.m_min.Z;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex.X = aabb.m_aabb.m_max.X;
                            vertex.Z = aabb.m_aabb.m_min.Z;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex.X = aabb.m_aabb.m_max.X;
                            vertex.Z = aabb.m_aabb.m_max.Z;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex.X = aabb.m_aabb.m_min.X;
                            vertex.Z = aabb.m_aabb.m_max.Z;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));



                            e.Graphics.FillPolygon(cornflowerbluebrush, vertices.ToArray());
                            e.Graphics.DrawPolygon(magentaPen, vertices.ToArray());

                            vertices.Clear();
                            break;

                        case COLLISION_OBJECT_TYPES.COLLISION_OBJECT_OBB:
                            CCollision_OBB obb = (CCollision_OBB)allColliders[i];

                            vertex = obb.m_obb.m_p0;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex = obb.m_obb.m_p2;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex = obb.m_obb.m_p3;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            vertex = obb.m_obb.m_p1;
                            vertices.Add(convertToScreenCoords(drawingZone, destRect, vertex.X, vertex.Z, zoomAmount));

                            e.Graphics.FillPolygon(cornflowerbluebrush, vertices.ToArray());
                            e.Graphics.DrawPolygon(magentaPen, vertices.ToArray());

                            vertices.Clear();
                            break;

                        default:
                            break;
                    }
                }
            }

            int numHorizontal = partitions.NumHorizontalPartitions;
            int numVertical = partitions.NumVerticalPartitions;
            double partitionSize = partitions.PartitionSize;
            double totalHeight = numVertical * partitionSize;
            double totalWidth = numHorizontal * partitionSize;
            for (int i = 0; i < numVertical + 1; i++)
            {
                PointF startPoint = convertToScreenCoords(drawingZone, destRect, startPos.X, startPos.Y + i * partitionSize, zoomAmount);
                PointF endPoint = convertToScreenCoords(drawingZone, destRect, startPos.X + totalWidth, startPos.Y + i * partitionSize, zoomAmount);
                e.Graphics.DrawLine(BlackPen, startPoint, endPoint);
            }
            for (int i = 0; i <  numHorizontal+ 1; i++)
            {
                PointF startPoint = convertToScreenCoords(drawingZone, destRect, startPos.X + i * partitionSize, startPos.Y, zoomAmount);
                PointF endPoint = convertToScreenCoords(drawingZone, destRect, startPos.X + i * partitionSize, startPos.Y + totalHeight, zoomAmount);
                e.Graphics.DrawLine(BlackPen, startPoint, endPoint);
            }

            //draw all mobs
            for (int i = 0; i < drawingZone.TheMobs.Length; i++)
            {
                if (drawingZone.TheMobs[i] != null)
                {
                    PointF screenPoint = convertToScreenCoords(drawingZone, destRect, drawingZone.TheMobs[i].CurrentPosition.m_position.X, drawingZone.TheMobs[i].CurrentPosition.m_position.Z, zoomAmount);
                    ServerControlledEntity currentMob = drawingZone.TheMobs[i];
                    string mobName = "[" + currentMob.ServerID + "]" + currentMob.Name;
                    if(currentMob.Level>0)
                    {
                        mobName+=" ("+currentMob.Level+")";
                    }
                    e.Graphics.DrawString(mobName, theFont, ShadowBrush, screenPoint.X + 2, screenPoint.Y + 2);
                    e.Graphics.DrawEllipse(new Pen(ShadowBrush), screenPoint.X + 1, screenPoint.Y + 1, 2.1f, 2.1f);

                    if (currentMob.InCombat && ((m_flashtoggle / 5) % 2) == 0)
                    {
                        e.Graphics.DrawString(mobName, theFont, Brushes.Blue, screenPoint.X + 1, screenPoint.Y + 1);
                        e.Graphics.DrawEllipse(BluePen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);
                    }
                    else if (currentMob.AggroRange > 0)
                    {

                        e.Graphics.DrawString(mobName, theFont, Brushes.Red, screenPoint.X + 1, screenPoint.Y + 1);
                        e.Graphics.DrawEllipse(RedPen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);
                    }
                    else if (currentMob.OpinionBase < 50)
                    {
                        e.Graphics.DrawString(mobName, theFont, Brushes.Orange, screenPoint.X + 1, screenPoint.Y + 1);
                        e.Graphics.DrawEllipse(YellowPen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);

                    }
                    else
                    {
                        e.Graphics.DrawString(mobName, theFont, Brushes.Green, screenPoint.X + 1, screenPoint.Y + 1);
                        e.Graphics.DrawEllipse(GreenPen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);
                    }
                }
            }
            for (int i = 0; i < drawingZone.m_players.Count; i++)
            {
                Player currentPlayer = drawingZone.m_players[i];
                Character currentCharacter = currentPlayer.m_activeCharacter;
                if (currentCharacter != null)
                {
                    PointF screenPoint = convertToScreenCoords(drawingZone, destRect, currentPlayer.m_activeCharacter.m_CharacterPosition.m_position.X, currentPlayer.m_activeCharacter.m_CharacterPosition.m_position.Z, zoomAmount);


                    if (currentPlayer.m_activeCharacter.InCombat && ((m_flashtoggle / 5) % 2) == 0)
                    {
                        e.Graphics.DrawString(currentPlayer.m_activeCharacter.m_name, theFont, Brushes.Blue, screenPoint.X, screenPoint.Y);
                        e.Graphics.DrawEllipse(BluePen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);
                    }
                    else
                    {

                        e.Graphics.DrawString(currentPlayer.m_activeCharacter.m_name, theFont, Brushes.Black, screenPoint.X, screenPoint.Y);
                        e.Graphics.DrawEllipse(BlackPen, screenPoint.X, screenPoint.Y, 2.1f, 2.1f);
                    }
                    if (currentCharacter.CurrentDuelTarget != null)
                    {
                        PointF duelPoint = convertToScreenCoords(drawingZone, destRect, currentCharacter.CurrentDuelTarget.DuelStartPos.X, currentCharacter.CurrentDuelTarget.DuelStartPos.Z, zoomAmount);

                        e.Graphics.DrawEllipse(AreaSelected, duelPoint.X, duelPoint.Y, 2.1f, 2.1f);
                    }
                    if (currentPlayer.m_activeCharacter.m_character_id == 134)
                    {
                        areaTestCharacter = currentPlayer.m_activeCharacter;

                        CharacterPath thePath = currentPlayer.m_activeCharacter.TheCharactersPath;
                        for (int currentPoint = 0; currentPoint < thePath.m_pastPath.Count; currentPoint++)
                        {
                            CharacterPathMarker currentMarker = thePath.m_pastPath[currentPoint];

                            Vector2 nextPoint = new Vector2(currentPlayer.m_activeCharacter.m_CharacterPosition.m_position.X, currentPlayer.m_activeCharacter.m_CharacterPosition.m_position.Z);
                            if (currentPoint + 1 < thePath.m_pastPath.Count)
                            {
                                CharacterPathMarker nextMarker = thePath.m_pastPath[currentPoint + 1];

                                nextPoint = new Vector2(nextMarker.Position.X, nextMarker.Position.Z);
                            }
                            PointF markerPoint = convertToScreenCoords(drawingZone, destRect, currentMarker.Position.X, currentMarker.Position.Z, zoomAmount);
                            e.Graphics.DrawEllipse(YellowPen, markerPoint.X, markerPoint.Y, 2.1f, 2.1f);
                        }
                        Vector3 combatPoint = thePath.GetRollbackPositionFromCurrentTime(Program.MainUpdateLoopStartTime(), currentPlayer.m_activeCharacter.m_CharacterPosition.m_position);
                        PointF combatScreenPoint = convertToScreenCoords(drawingZone, destRect, combatPoint.X, combatPoint.Z, zoomAmount);


                        e.Graphics.DrawEllipse(AreaSelected, combatScreenPoint.X, combatScreenPoint.Y, 2.1f, 2.1f);
                    }
                }
            }
            
            BlackPen.Dispose();
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            m_zoomLevel++;
            if (m_zoomLevel > 5)
            {
                m_zoomLevel = 5;
            }
            else
            {
                float zoomAmount = (float)Math.Pow(2, m_zoomLevel);
                m_mapOffsetX -= pn2Dmap.Width / (2 * zoomAmount);
                m_mapOffsetY -= pn2Dmap.Height / (2 * zoomAmount);

            }
            refreshButtons();

        }
        private void refreshButtons()
        {
            if (m_zoomLevel > 0)
                btnZoomOut.Enabled = true;
            else
                btnZoomOut.Enabled = false;
            if (m_zoomLevel < 3)
                btnZoomIn.Enabled = true;
            else
                btnZoomIn.Enabled = false;
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            m_zoomLevel--;
            if (m_zoomLevel < 0)
            {
                m_zoomLevel = 0;
            }
            else
            {
                float zoomAmount = (float)Math.Pow(2, m_zoomLevel + 1);
                m_mapOffsetX += pn2Dmap.Width / (2 * zoomAmount);
                m_mapOffsetY += pn2Dmap.Height / (2 * zoomAmount);

            }
            refreshButtons();

        }

        private void pn2Dmap_MouseDown(object sender, MouseEventArgs e)
        {
            m_oldMouseX = e.X;
            m_oldMouseY = e.Y;
            m_mouseDown = true;
        }

        private void pn2Dmap_MouseMove(object sender, MouseEventArgs e)
        {
            float zoomAmount = (float)Math.Pow(2, m_zoomLevel);
            if (m_mouseDown)
            {
                m_mapOffsetX -= (m_oldMouseX - e.X) / zoomAmount;
                m_mapOffsetY -= (m_oldMouseY - e.Y) / zoomAmount;
                m_oldMouseX = e.X;
                m_oldMouseY = e.Y;
            }
        }

        private void pn2Dmap_MouseUp(object sender, MouseEventArgs e)
        {
            float zoomAmount = (float)Math.Pow(2, m_zoomLevel);
            if (m_mouseDown)
            {
                m_mapOffsetX -= (m_oldMouseX - e.X) / zoomAmount;
                m_mapOffsetY -= (m_oldMouseY - e.Y) / zoomAmount;
                m_oldMouseX = e.X;
                m_oldMouseY = e.Y;
                m_mouseDown = false;
            }
        }



        private void tbMaxUsers_TextChanged(object sender, EventArgs e)
        {
            int oldmaxusers = Program.m_max_users;
            try
            {
                Program.m_max_users = Int32.Parse(tbMaxUsers.Text);
            }
            catch (Exception)
            {
                Program.m_max_users = oldmaxusers;
            }
        }
        /*
        private void Form1_Resize(object sender, EventArgs e)
        {
            Program.Display("resized");
        }

        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            Program.Display("resized");
        }*/

        private void btnSendAlert_Click(object sender, EventArgs e)
        {
            SystemMessageForm msgform = new SystemMessageForm();
            msgform.ShowDialog();
        }

        private void txtUpdateTime_TextChanged(object sender, EventArgs e)
        {
            double newPeriod = 50;
            if (double.TryParse(txtUpdateTime.Text, out newPeriod))
            {
                if (newPeriod < 0)
                    newPeriod = 0;

            }
            Program.m_worldUpdatePeriod = newPeriod;
            txtUpdateTime.Text = newPeriod.ToString();
        }

        private void txtMaxDequeue_TextChanged(object sender, EventArgs e)
        {
            int newAmount = 1000;
            if (Int32.TryParse(txtMaxDequeue.Text, out newAmount))
            {
                if (newAmount < 0)
                    newAmount = 0;

            }
            Program.m_maxMessagesDequeue = newAmount;
            txtMaxDequeue.Text = newAmount.ToString();
        }

        private void btnKick_Click(object sender, EventArgs e)
        {
            List<Player> playersList = new List<Player>();
            string playerString = "";
            for (int i = 0; i < lvPlayerList.Items.Count; i++)
            {

                if (lvPlayerList.Items[i].Selected && lvPlayerList.Items[i].Tag != null)
                {
                    Player player = (Player)lvPlayerList.Items[i].Tag;
                    playersList.Add(player);
                    playerString += "," + player.m_UserName;
                }

            }
            if (playerString.Length > 0)
            {
                playerString = playerString.Substring(1);
            }
            if (playersList.Count > 0)
            {
                KickConfirm kickConfirmForm = new KickConfirm();

                kickConfirmForm.m_kickPlayers = playersList;
                kickConfirmForm.lblKickList.Text = "Are you Sure you want to kick " + playerString + " ?";
                kickConfirmForm.Show();
            }
        }

        private void btnRelocate_Click(object sender, EventArgs e)
        {
            List<Player> playerList = new List<Player>();
            string playerListStr = "";
            for (int i = 0; i < lvPlayerList.Items.Count; i++)
            {

                if (lvPlayerList.Items[i].Selected && lvPlayerList.Items[i].Tag != null)
                {
                    Player player = (Player)lvPlayerList.Items[i].Tag;

                    if (player.m_activeCharacter != null)
                    {
                        playerList.Add(player);
                        playerListStr += "," + player.m_UserName;
                    }

                }

            }
            if (playerListStr.Length > 0)
            {
                playerListStr = playerListStr.Substring(1);
            }
            if (playerList.Count > 0)
            {
                RelocateConfirm relocateConfirmForm = new RelocateConfirm();

                relocateConfirmForm.m_RelocatePlayers = playerList;
                relocateConfirmForm.lblRelocateList.Text = "Are you Sure you want to relocate " + playerListStr + " ?";
                relocateConfirmForm.Show();
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_currentTab = tabControl1.SelectedIndex;
        }

        internal void AddZoneToComboBox(string in_zone)
        {
            if (cbZone.IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    cbZone.Invoke(new Action<string>(AddZoneToComboBox), in_zone);
                }
                catch
                {
                }

                return;
            }

            if(cbZone.Items.Contains(in_zone) == false)
                cbZone.Items.Add(in_zone);
        }

        internal void SelectZoneComboBox(int in_selectedIndex)
        {
            if (cbZone.IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    cbZone.Invoke(new Action<int>(SelectZoneComboBox), in_selectedIndex);
                }
                catch
                {
                }

                return;
            }

            cbZone.SelectedIndex = in_selectedIndex;
        }

        internal void UpdateCurrentUsers(string in_currentUsers)
        {
            if (tbCurrentUsers.IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    tbCurrentUsers.Invoke(new Action<string>(UpdateCurrentUsers), in_currentUsers);
                }
                catch
                {
                }

                return;
            }

            tbCurrentUsers.Text = in_currentUsers;
        }

        internal void UpdateTitle(string in_title)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    Invoke(new Action<string>(UpdateTitle), in_title);
                }
                catch
                {
                }

                return;
            }

            Text = in_title;
        }

        internal void UpdateStatusDetails()
        {
            if (labelMainUpdateTime == null)
                return;

            if (labelMainUpdateTime.IsDisposed)
                return;

            if (labelMainUpdateTime.InvokeRequired == true)
            {
                try
                {
                    Invoke(new Action(UpdateStatusDetails));
                }
                catch
                {
                }

                return;
            }

            Program.UpdateStatusDetails();
        }
        
        internal void UpdateDiagnosticBackgroundTaskName(double in_elapsedSeconds)
        {
            if (labelActiveTask == null)
                return;

            if (labelActiveTask.IsDisposed)
                return;

            if (labelActiveTask.InvokeRequired == true)
            {
                try
                {
                    Invoke(new Action<double>(UpdateDiagnosticBackgroundTaskName), in_elapsedSeconds);
                }
                catch
                {
                }

                return;
            }

            Program.UpdateDiagnosticBackgroundTaskName(in_elapsedSeconds);
        }

        public void updatePanel(int zone)
        {
            if (m_currentTab == 2 && m_drawnZoneIndex > 0 && zone == Program.processor.m_zones[m_drawnZoneIndex - 1].m_zone_id)
            {
                pn2Dmap.Invalidate();
            }
        }

        private void btnCreatePlayers_Click(object sender, EventArgs e)
        {
           Program.processor.CreatePlayers();
            //Program.processor.createPlayers2();
        }

        private void btnLogOptions_Click(object sender, EventArgs e)
        {
            LogOptionsForm optionsForm = new LogOptionsForm();
            optionsForm.Show();
        }

        private void btnReinitialiseThirdPartyOptions_Click(object sender, EventArgs e)
        {
            Program.ReinitialiseThirdPartyOptions();
        }

        private void chkRemoveAllMobs_Click(object sender, EventArgs e)
        {
            Program.m_RemoveAllMobs = chkRemoveAllMobs.Checked;
        }

		private void buttonSpecialOffers_Click(object sender, EventArgs e)
		{
			
			if (SpecialOfferTemplate.SPECIAL_OFFERS_ACTIVE == true)
			{
				SpecialOfferTemplateManager.ClearTemplates();
				SpecialOfferTemplateManager.LoadSpecialOfferTemplates(Program.processor.m_dataDB);
				Program.processor.m_globalOfferManager = new SpecialOfferManager();
				Program.processor.m_globalOfferManager.LoadGlobalOffers(Program.processor.m_dataDB);
				Program.Display("Reloaded Special Offers ");				
			}
			else
			{
				Program.Display("Special Offers not Loaded, SPECIAL_OFFERS_ACTIVE off");
			}
		}

        private void ckIngameMap_CheckedChanged(object sender, EventArgs e)
        {
            pn2Dmap.Invalidate();
        }

        private void AHRadioButtonOffline_CheckedChanged(object sender, EventArgs e)
        {
            if (Program.processor.TheAuctionHouse != null && AHRadioButtonOffline.Checked)
            {
                if (AHResetTimeSet == false)
                {
                    AHResetTimeSet = true;
                    AHResetTime = DateTime.Now;
                }
                Program.Display("AUCTION HOUSE STATUS - OFFLINE");
                Program.m_auctionHouseActive = 0;
                Program.processor.TheAuctionHouse.SetAuctionHouseStatus(AuctionHouse.Enums.AHStatus.OFFLINE, AHResetTime);
                resetDurationsCheckBox.Checked = false;
            }

            AuctionHouseStatus = 0;
        }

        private void AHRadioButtonSafeMode_CheckedChanged(object sender, EventArgs e)
        {
            if (Program.processor.TheAuctionHouse != null && AHRadioButtonSafeMode.Checked)
            {
                if (AHResetTimeSet == false)
                {
                    AHResetTimeSet = true;
                    AHResetTime = DateTime.Now;
                }
                Program.Display("AUCTION HOUSE STATUS - SAFE MODE");
                Program.m_auctionHouseActive = 1;
                Program.processor.TheAuctionHouse.SetAuctionHouseStatus(AuctionHouse.Enums.AHStatus.SAFE_MODE, AHResetTime);
                resetDurationsCheckBox.Checked = false;
            }

            AuctionHouseStatus = 1;
        }

        private void AHRadioButtonOnline_CheckedChanged(object sender, EventArgs e)
        {
            if (Program.processor.TheAuctionHouse != null && AHRadioButtonOnline.Checked)
            {
                AHResetTimeSet = false;
                Program.Display("AUCTION HOUSE STATUS - ONLINE");
                Program.m_auctionHouseActive = 2;
                Program.processor.TheAuctionHouse.SetAuctionHouseStatus(AuctionHouse.Enums.AHStatus.ONLINE, AHResetTime, resetDurationsCheckBox.Checked);
            }

            AuctionHouseStatus = 2;
        }

        /// <summary>
        /// Set the icon for this app based on server name (want apple & android worlds plus a default)
        /// </summary>
        /// <param name="serverName"></param>
        public void SetIcon(string serverName)
        {
            //lowercase and trim whitespace
            serverName = serverName.ToLower();
            serverName = serverName.Trim();

            //android servers
            if(serverName =="balor")
                this.Icon = Properties.Resources.androidBalor;
            else if (serverName == "donn")
                this.Icon = Properties.Resources.androidDonn;
            else if (serverName == "fingal")
                this.Icon = Properties.Resources.androidFingal;
            else if (serverName == "lir")
                this.Icon = Properties.Resources.androidLir;
            else if (serverName == "google")
                this.Icon = Properties.Resources.androidPreProd;
            else if (serverName == "android beta")
                this.Icon = Properties.Resources.androidBeta;
            //apple servers
            else if (serverName == "arawn") 
                this.Icon = Properties.Resources.appleArawn;
            else if (serverName == "belenus")
                this.Icon = Properties.Resources.appleBelenus;
            else if (serverName == "crom")
                this.Icon = Properties.Resources.appleCrom;
            else if (serverName == "danu")
                this.Icon = Properties.Resources.appleDanu;
            else if (serverName == "epona")
                this.Icon = Properties.Resources.appleEpona;
            else if (serverName == "gwydion")
                this.Icon = Properties.Resources.appleGwydion;
            else if (serverName == "herne")
                this.Icon = Properties.Resources.appleHerne;
            else if (serverName == "lugh")
                this.Icon = Properties.Resources.appleLugh;
            else if (serverName == "mabon")
                this.Icon = Properties.Resources.appleMabon;
            else if (serverName == "morrigan")
                this.Icon = Properties.Resources.appleMorrigan;
            else if (serverName == "rhiannon")
                this.Icon = Properties.Resources.appleRhiannon;
            else if (serverName == "rosmerta")
                this.Icon = Properties.Resources.appleRosmerta;
            else if (serverName == "sulis")
                this.Icon = Properties.Resources.appleSulis;
            else if (serverName == "taranis")
                this.Icon = Properties.Resources.appleTaranis;
            else if (serverName == "apple")
                this.Icon = Properties.Resources.applePreProd;
            else if (serverName == "ios beta")
                this.Icon = Properties.Resources.appleBeta;
            else //default
                this.Icon = Properties.Resources.allRpg;            
        }

        public void Display(string text)
        {
			lock (lockObjLogs)
			{
				sbLogs.AppendLine(text);
				sbBuffer.AppendLine(text);
				++logEntryCount;
			}
		}

		public void DisplayText(string text)
		{
			if (text == null)
				return;

			if (richTextBox1 == null)
				return;

			if (richTextBox1.IsDisposed)
				return;

			if (richTextBox1.InvokeRequired == true)
			{
                try
                {
                    richTextBox1.BeginInvoke(new Action<string>(DisplayText), text);
                }
                catch
                {
                }

				return;
			}


			richTextBox1.AppendText(text);
		}

		public void SetDisplayText(string text)
		{
			if (text == null)
				return;

			if (richTextBox1 == null)
				return;

			if (richTextBox1.IsDisposed)
				return;

			if (richTextBox1.InvokeRequired == true)
			{
                try
                {
                    richTextBox1.BeginInvoke(new Action<string>(SetDisplayText), text);
                }
                catch
                {
                }

				return;
			}

			richTextBox1.Text = text;
		}

		public string GetPendingText()
		{
			lock (lockObjLogs)
			{
				string ret = sbLogs.ToString();
				sbLogs.Length = 0;

				return ret;
			}
		}

		public void RemoveOldText()
		{
			if (logEntryCount >= kMaxEntryToKeep)
			{
				string ret = String.Empty;
				lock (lockObjLogs)
				{
					ret = sbBuffer.ToString();
					sbBuffer.Length = 0;
					logEntryCount = 0;
				}

				SetDisplayText(ret);
			}
		}

		/// <summary>
		/// Remove focus so that the text does NOT auto scroll
		/// </summary>
		public void RemoveFocusFromRichTextBox()
        {
            label1.Focus();
            this.ActiveControl = label1;
        }

        /// <summary>
        /// Take focus, text box WILL now auto scroll
        /// </summary>
        internal void AddFocusToRichTextBox()
        {
            richTextBox1.Focus();
            this.ActiveControl = richTextBox1;
        }

        internal void SetAuctionOffline(bool flag)
        {
            if (AHRadioButtonOffline == null)
                return;

            if (AHRadioButtonOffline.IsDisposed)
                return;

            if (AHRadioButtonOffline.InvokeRequired == true)
            {
                AHRadioButtonOffline.Invoke(new Action<bool>(SetAuctionOffline), flag);
                return;
            }

            AHRadioButtonOffline.Checked = flag;
        }

        internal void SetAuctionSafe(bool flag)
        {
            if (AHRadioButtonSafeMode == null)
                return;

            if (AHRadioButtonSafeMode.IsDisposed)
                return;

            if (AHRadioButtonSafeMode.InvokeRequired == true)
            {
                AHRadioButtonSafeMode.Invoke(new Action<bool>(SetAuctionSafe), flag);
                return;
            }

            AHRadioButtonSafeMode.Checked = flag;
        }


        internal void SetAuctionOnline(bool flag)
        {
            if (AHRadioButtonOnline == null)
                return;

            if (AHRadioButtonOnline.IsDisposed)
                return;

            if (AHRadioButtonOnline.InvokeRequired == true)
            {
                AHRadioButtonOnline.Invoke(new Action<bool>(SetAuctionOnline), flag);
                return;
            }

            AHRadioButtonOnline.Checked = flag;
        }
        
        private void RewardsEnabled_CheckedChanged(object sender, EventArgs e)
        {
            DailyRewardManager.DAILY_REWARDS_ACTIVE = RewardsEnabled.Checked;
            Program.Display("Daily Reward Availability is now set to " + DailyRewardManager.DAILY_REWARDS_ACTIVE.ToString());
        }

        private void ReloadRewards_Click(object sender, EventArgs e)
        {
            DailyRewardTemplateManager.LoadDailyRewards(Program.processor.m_dataDB);
            Program.Display("Daily Login Rewards reloaded");
        }

        private void checkBoxPerformanceLogging_CheckedChanged(object sender, EventArgs e)
        {
            Program.processor.EnablePerformanceLogging(checkBoxPerformanceLogging.Checked);
        }
        
        internal void StartPlayerRefreshTimer()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    Invoke(new Action(StartPlayerRefreshTimer));
                }
                catch
                {
                }

                return;
            }

            refreshPlayersTimer = new Timer();
            refreshPlayersTimer.Interval = 5000;
            refreshPlayersTimer.Start();
            refreshPlayersTimer.Tick += new EventHandler(Program.refreshPlayersTimer_Tick);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Program.processor != null)
            {
                //Grab the appropriate season profile from the dictionary(as chosen by the form combobox)
                List<string> seasonProfile = seasonDictionary[(comboBox1.SelectedItem as ComboboxItem).Value.ToString()];

                //Set the profile
                Program.processor.SetSeasonTweak(seasonProfile);

                //Save the new profile setting to app.config
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                if (ConfigurationManager.AppSettings["currentSeason"] != null)
                {
                    config.AppSettings.Settings.Remove("currentSeason");
                    config.AppSettings.Settings.Add("currentSeason", (comboBox1.SelectedItem as ComboboxItem).Value.ToString());
                }
                else
                {
                    MessageBox.Show("Key: 'currentSeason' is missing from exe.config, please add key.","File Missing", MessageBoxButtons.OK);
                }
                

                config.Save(ConfigurationSaveMode.Minimal);  
            }
        }

        private void buttonReloadAdminList_Click(object sender, EventArgs e)
        {
            ReloadAdminList();
        }

        private void ReloadAdminList()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired == true)
            {
                try
                {
                    Invoke(new Action(ReloadAdminList));

                }
                catch{}

                return;
            }

            Program.processor.ReloadAdminsList();

            Program.Display("Reloaded Admin List");
        }

        private void buttonReloadLocalisation_Click(object sender, EventArgs e)
        {
            Program.Display("Reloaded Localisation Data");
            Localise.Localiser.ResetTextDB();
        }
    }
}
