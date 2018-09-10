using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using XnaGeometry;

namespace AStarMesh
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void nextStepButton_Click(object sender, EventArgs e)
        {
            m_pathFinder.NextStep();
            mapView.Invalidate();
        }
        private void resetButton_Click(object sender, EventArgs e)
        {
            m_pathFinder.Reset();
            Vector3 startPoint = new Vector3(0);
            Vector3 endPoint = new Vector3(0);
            startPoint.X = (float)Convert.ToDouble(startPosX.Text);
            startPoint.Y = (float)Convert.ToDouble(startPosY.Text);
            startPoint.Z = (float)Convert.ToDouble(startPosZ.Text);

            endPoint.X = (float)Convert.ToDouble(endPosX.Text);
           endPoint.Y = (float)Convert.ToDouble(endPosY.Text);
            endPoint.Z = (float)Convert.ToDouble(endPosZ.Text);
            m_pathFinder.pather.SetUpForSearch(startPoint, endPoint);
            mapView.Invalidate();
        }

        private void mapView_Paint(object sender, PaintEventArgs e)
        {
            ASMap theMap = m_pathFinder.TheMap;
            if (theMap == null)
            {
                return;
            }

            int minX = theMap.MinX;
            int maxX = theMap.MaxX - (theMap.MaxX-theMap.MinX) * 3 / 4;
            int minY = theMap.MinY;
            int maxY = theMap.MaxY - (theMap.MaxY - theMap.MinY) * 3 / 4;

            float xRange = maxX - minX;
            float yRange = maxY - minY;

            float xScale = (float)mapView.Width / xRange;
            float yScale = (float)mapView.Height/ yRange;

            float scale = xScale;
            if (xScale > yScale)
            {
                scale = yScale;
            }

            //Pen BlackPen = new Pen(Brushes.Black);
            //BlackPen.Width = 2.1f;
            Pen RedPen = new Pen(Brushes.Red);
            RedPen.Width = 2.1f;
            Pen GreenPen = new Pen(Brushes.Green);
            GreenPen.Width = 2.1f;
            Pen YellowPen = new Pen(Brushes.Orange);
            YellowPen.Width = 2.1f;
            Pen BluePen = new Pen(Brushes.Blue);
            BluePen.Width = 2.1f;
            Pen WhitePen = new Pen(Brushes.White);
            WhitePen.Width = 2.1f;

            Pen TanPen = new Pen(Brushes.Tan);
            TanPen.Width = 2.1f;
            float addAmount = 0;
            Pen TealPen = new Pen(Brushes.Teal);
            TealPen.Width = 2.1f;
            Pen MagPen = new Pen(Brushes.Magenta);
            MagPen.Width = 2.6f;

            Brush ShadowBrush = new SolidBrush(Color.FromArgb(128, 64, 64, 64));
            Pen currentPen = WhitePen;
            List<ASTriangle> triangles = theMap.Triangles;
            //draw all triangles
            for (int i = 0; i < triangles.Count; i++)
            {
                ASTriangle currentTriangle = triangles[i];


                Vector3 scaledPosition0 = new Vector3((currentTriangle.GetVertices(0).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(0).Z - minY) * scale);
                Vector3 scaledPosition1 = new Vector3((currentTriangle.GetVertices(1).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(1).Z - minY) * scale);
                Vector3 scaledPosition2 = new Vector3((currentTriangle.GetVertices(2).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(2).Z - minY) * scale);

                scaledPosition0.Z += addAmount;
                scaledPosition1.Z += addAmount;
                scaledPosition2.Z += addAmount;

                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition1.X, (float)scaledPosition1.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.1f, 2.1f);

                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z), new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z), new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z));
            }

            //draw open list
            currentPen = GreenPen;
            List<ASNode> openList = m_pathFinder.pather.OpenList;
            for (int i = 0; i < openList.Count; i++)
            {
                ASNode currentNode = openList[i];
                ASTriangle currentTriangle = currentNode.Triangle;


                Vector3 scaledPosition0 = new Vector3((currentTriangle.GetVertices(0).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(0).Z - minY) * scale);
                Vector3 scaledPosition1 = new Vector3((currentTriangle.GetVertices(1).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(1).Z - minY) * scale);
                Vector3 scaledPosition2 = new Vector3((currentTriangle.GetVertices(2).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(2).Z - minY) * scale);

                scaledPosition0.Z += addAmount;
                scaledPosition1.Z += addAmount;
                scaledPosition2.Z += addAmount;
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition1.X, (float)scaledPosition1.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.1f, 2.1f);

                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z), new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z), new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z));

                Vector3 EntryPosition0 = new Vector3((currentNode.EntryPoint.X - minX) * scale, 0, mapView.Height - (currentNode.EntryPoint.Z - minY) * scale);
                EntryPosition0.Z += addAmount;
                e.Graphics.DrawEllipse(MagPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.6f, 2.6f);
            
            }
            //draw closed list
            currentPen = YellowPen;
            List<ASNode> closedList = m_pathFinder.pather.ClosedList;
            for (int i = 0; i < closedList.Count; i++)
            {
                ASNode currentNode = closedList[i];
                ASTriangle currentTriangle = currentNode.Triangle;


                Vector3 scaledPosition0 = new Vector3((currentTriangle.GetVertices(0).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(0).Z - minY) * scale);
                Vector3 scaledPosition1 = new Vector3((currentTriangle.GetVertices(1).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(1).Z - minY) * scale);
                Vector3 scaledPosition2 = new Vector3((currentTriangle.GetVertices(2).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(2).Z - minY) * scale);

                scaledPosition0.Z += addAmount;
                scaledPosition1.Z += addAmount;
                scaledPosition2.Z += addAmount;

                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition1.X, (float)scaledPosition1.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.1f, 2.1f);

                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z), new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z), new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z));

            }
            for (int i = 0; i < openList.Count; i++)
            {
                ASNode currentNode = openList[i];
                Vector3 EntryPosition0 = new Vector3((currentNode.EntryPoint.X - minX) * scale, 0, mapView.Height - (currentNode.EntryPoint.Z - minY) * scale);
                EntryPosition0.Z += addAmount;
                e.Graphics.DrawEllipse(WhitePen, (float)EntryPosition0.X, (float)EntryPosition0.Z, 2.6f, 2.6f);

            }
            for (int i = 0; i < closedList.Count; i++)
            {
                ASNode currentNode = closedList[i];
                Vector3 EntryPosition0 = new Vector3((currentNode.EntryPoint.X - minX) * scale, 0, mapView.Height - (currentNode.EntryPoint.Z - minY) * scale);
                EntryPosition0.Z += addAmount;
                e.Graphics.DrawEllipse(WhitePen, (float)EntryPosition0.X, (float)EntryPosition0.Z, 2.6f, 2.6f);

            }
            //draw startPoint
            currentPen = BluePen;
            {
                Vector3 startPoint = m_pathFinder.pather.StartPoint;
                Vector3 scaledPosition0 = new Vector3((startPoint.X - minX) * scale, 0, mapView.Height - (startPoint.Z - minY) * scale);
                scaledPosition0.Z += addAmount;
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
            }
            //draw start triangle
           
            ASTriangle startTri = m_pathFinder.pather.StartingTriangle;
            if (startTri != null)
            {
                ASTriangle currentTriangle = startTri;


                Vector3 scaledPosition0 = new Vector3((currentTriangle.GetVertices(0).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(0).Z - minY) * scale);
                Vector3 scaledPosition1 = new Vector3((currentTriangle.GetVertices(1).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(1).Z - minY) * scale);
                Vector3 scaledPosition2 = new Vector3((currentTriangle.GetVertices(2).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(2).Z - minY) * scale);

                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition1.X, (float)scaledPosition1.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.1f, 2.1f);
                scaledPosition0.Z += addAmount;
                scaledPosition1.Z += addAmount;
                scaledPosition2.Z += addAmount;

                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z), new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z), new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z));

            }
            //draw end point
            currentPen = RedPen;
            {
                Vector3 endPoint = m_pathFinder.pather.EndPoint;
                Vector3 scaledPosition0 = new Vector3((endPoint.X - minX) * scale, 0, mapView.Height - (endPoint.Z - minY) * scale);
                scaledPosition0.Z += addAmount;

                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
            }
            //draw end triangle

            ASTriangle endTri = m_pathFinder.pather.EndTriangle;
            if (endTri != null)
            {
                ASTriangle currentTriangle = endTri;


                Vector3 scaledPosition0 = new Vector3((currentTriangle.GetVertices(0).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(0).Z - minY) * scale);
                Vector3 scaledPosition1 = new Vector3((currentTriangle.GetVertices(1).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(1).Z - minY) * scale);
                Vector3 scaledPosition2 = new Vector3((currentTriangle.GetVertices(2).X - minX) * scale, 0, mapView.Height - (currentTriangle.GetVertices(2).Z - minY) * scale);

                scaledPosition0.Z += addAmount;
                scaledPosition1.Z += addAmount;
                scaledPosition2.Z += addAmount;

                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition1.X, (float)scaledPosition1.Z, 2.1f, 2.1f);
                e.Graphics.DrawEllipse(currentPen, (float)scaledPosition2.X, (float)scaledPosition2.Z, 2.1f, 2.1f);

                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z), new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z));
                e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition2.X, (float)scaledPosition2.Z), new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z));

            }
            //draw Path
            currentPen = TanPen;
            List<Vector3> path = m_pathFinder.pather.InnitialPath;
            if (path != null)
            {

                for (int i = 1; i < path.Count; i++)
                {
                    Vector3 scaledPosition0 = new Vector3((path[i - 1].X - minX) * scale, 0, mapView.Height - (path[i - 1].Z - minY) * scale);
                    Vector3 scaledPosition1 = new Vector3((path[i].X - minX) * scale, 0, mapView.Height - (path[i].Z - minY) * scale);
                    scaledPosition0.Z += addAmount;
                    scaledPosition1.Z += addAmount;

                    e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                    e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));
              
                }
            }

            //draw short Path
            currentPen = TealPen;
            List<Vector3> shortpath = m_pathFinder.pather.Path;
            if (shortpath != null)
            {

                for (int i = 1; i < shortpath.Count; i++)
                {
                    Vector3 scaledPosition0 = new Vector3((shortpath[i - 1].X - minX) * scale, 0, mapView.Height - (shortpath[i - 1].Z - minY) * scale);
                    Vector3 scaledPosition1 = new Vector3((shortpath[i].X - minX) * scale, 0, mapView.Height - (shortpath[i].Z - minY) * scale);
                    scaledPosition0.Z += addAmount;
                    scaledPosition1.Z += addAmount;
                    e.Graphics.DrawEllipse(currentPen, (float)scaledPosition0.X, (float)scaledPosition0.Z, 2.1f, 2.1f);
                    e.Graphics.DrawLine(currentPen, new PointF((float)scaledPosition0.X, (float)scaledPosition0.Z), new PointF((float)scaledPosition1.X, (float)scaledPosition1.Z));

                }
            }

        }

        private void endPosX_TextChanged(object sender, EventArgs e)
        {

        }

        private void startPosX_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
