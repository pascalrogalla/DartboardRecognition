﻿#region Usings

using System;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Point = System.Drawing.Point;

#endregion

namespace DartboardRecognition
{
    public class Measureman
    {
        private MainWindow view;
        private Drawman drawman;
        private Storage storage;
        private Rectangle roiRectangle;
        private Moments contourMoments;
        private Point contourCenterPoint;
        private Point contourBoxPoint1;
        private Point contourBoxPoint2;
        private Point contourBoxPoint3;
        private Point contourBoxPoint4;
        private Point contourBoxMiddlePoint1;
        private Point contourBoxMiddlePoint2;
        private Point spikeLinePoint1;
        private Point spikeLinePoint2;
        private Point projectionCenterPoint;
        private Point projectionLineCam1Point1;
        private Point projectionLineCam1Point2;
        private Point projectionLineCam2Point1;
        private Point projectionLineCam2Point2;
        private Point? camPoi;
        private Image<Bgr, byte> dartboardProjectionFrame;
        private Image<Bgr, byte> dartboardWorkingProjectionFrame;
        private int projectionLineCam1Bias = 0;
        private int projectionLineCam2Bias = 0;
        private Point cam1SetupPoint;
        private Point cam2SetupPoint;
        private int minContourArcLength = 190;
        private int maxContourArcLength = 600;
        private Cam workingCam;

        public Measureman(MainWindow view, Drawman drawman)
        {
            this.view = view;
            this.drawman = drawman;
            storage = new Storage();
        }

        public void SetupWorkingCam(Cam cam)
        {
            workingCam = cam;
        }

        public void CalculateSetupLines()
        {
            workingCam.linedFrame = workingCam.originFrame.Clone();

            roiRectangle = new Rectangle((int) workingCam.roiPosXSlider,
                                         (int) workingCam.roiPosYSlider,
                                         (int) workingCam.roiWidthSlider,
                                         (int) workingCam.roiHeightSlider);
            drawman.DrawRectangle(workingCam.linedFrame, roiRectangle, view.CamRoiRectColor.MCvScalar, view.CamRoiRectThickness);

            workingCam.surfacePoint1 = new Point(0, (int) workingCam.surfaceSlider);
            workingCam.surfacePoint2 = new Point(workingCam.originFrame.Cols, (int) workingCam.surfaceSlider);
            drawman.DrawLine(workingCam.linedFrame, workingCam.surfacePoint1, workingCam.surfacePoint2, view.CamSurfaceLineColor.MCvScalar, view.CamSurfaceLineThickness);

            workingCam.surfaceCenterPoint1 = new Point
                                             {
                                                 X = (int) workingCam.surfaceCenterSlider,
                                                 Y = (int) workingCam.surfaceSlider
                                             };
            workingCam.surfaceCenterPoint2 = new Point
                                             {
                                                 X = workingCam.surfaceCenterPoint1.X,
                                                 Y = workingCam.surfaceCenterPoint1.Y - 50
                                             };
            drawman.DrawLine(workingCam.linedFrame, workingCam.surfaceCenterPoint1, workingCam.surfaceCenterPoint2, view.CamSurfaceLineColor.MCvScalar, view.CamSurfaceLineThickness);

            workingCam.surfaceLeftPoint1 = new Point
                                           {
                                               X = (int) workingCam.surfaceLeftSlider,
                                               Y = (int) workingCam.surfaceSlider
                                           };
            workingCam.surfaceLeftPoint2 = new Point
                                           {
                                               X = workingCam.surfaceLeftPoint1.X,
                                               Y = workingCam.surfaceLeftPoint1.Y - 50
                                           };
            drawman.DrawLine(workingCam.linedFrame, workingCam.surfaceLeftPoint1, workingCam.surfaceLeftPoint2, view.CamSurfaceLineColor.MCvScalar, view.CamSurfaceLineThickness);

            workingCam.surfaceRightPoint1 = new Point
                                            {
                                                X = (int) workingCam.surfaceRightSlider,
                                                Y = (int) workingCam.surfaceSlider
                                            };
            workingCam.surfaceRightPoint2 = new Point
                                            {
                                                X = workingCam.surfaceRightPoint1.X,
                                                Y = workingCam.surfaceRightPoint1.Y - 50
                                            };
            drawman.DrawLine(workingCam.linedFrame, workingCam.surfaceRightPoint1, workingCam.surfaceRightPoint2, view.CamSurfaceLineColor.MCvScalar, view.CamSurfaceLineThickness);
        }

        public void CalculateRoiRegion()
        {
            workingCam.roiFrame = workingCam.originFrame.Clone();
            workingCam.roiFrame.ROI = roiRectangle;
        }

        public void CalculateDartboardProjection()
        {
            // Draw dartboard projection
            dartboardProjectionFrame = new Image<Bgr, byte>(view.ProjectionFrameWidth, view.ProjectionFrameHeight);
            projectionCenterPoint = new Point(dartboardProjectionFrame.Width / 2, dartboardProjectionFrame.Height / 2);
            cam1SetupPoint = new Point(30, 30);
            cam2SetupPoint = new Point(dartboardProjectionFrame.Cols - 30, 30);

            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 7, view.ProjectionGridColor, view.ProjectionGridThickness);
            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 17, view.ProjectionGridColor, view.ProjectionGridThickness);
            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 95, view.ProjectionGridColor, view.ProjectionGridThickness);
            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 105, view.ProjectionGridColor, view.ProjectionGridThickness);
            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 160, view.ProjectionGridColor, view.ProjectionGridThickness);
            drawman.DrawCircle(dartboardProjectionFrame, projectionCenterPoint, view.ProjectionCoefficent * 170, view.ProjectionGridColor, view.ProjectionGridThickness);
            for (var i = 0; i <= 360; i += 9)
            {
                var segmentPoint1 = new Point
                                    {
                                        X = (int) (projectionCenterPoint.X + Math.Cos(0.314159 * i - 0.15708) * view.ProjectionCoefficent * 170),
                                        Y = (int) (projectionCenterPoint.Y + Math.Sin(0.314159 * i - 0.15708) * view.ProjectionCoefficent * 170)
                                    };
                var segmentPoint2 = new Point
                                    {
                                        X = (int) (projectionCenterPoint.X + Math.Cos(0.314159 * i - 0.15708) * view.ProjectionCoefficent * 17),
                                        Y = (int) (projectionCenterPoint.Y + Math.Sin(0.314159 * i - 0.15708) * view.ProjectionCoefficent * 17)
                                    };
                drawman.DrawLine(dartboardProjectionFrame, segmentPoint1, segmentPoint2, view.ProjectionGridColor, view.ProjectionGridThickness);
            }

            // Draw surface projection lines
            projectionLineCam1Point1.X = (int) (projectionCenterPoint.X + Math.Cos(-0.785398) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam1Bias;
            projectionLineCam1Point1.Y = (int) (projectionCenterPoint.Y + Math.Sin(-0.785398) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam1Bias;
            projectionLineCam1Point2.X = (int) (projectionCenterPoint.X + Math.Cos(2.35619) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam1Bias;
            projectionLineCam1Point2.Y = (int) (projectionCenterPoint.Y + Math.Sin(2.35619) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam1Bias;
            //drawman.DrawLine(dartboardProjectionFrame, projectionLineCam1Point1, projectionLineCam1Point2, view.ProjectionSurfaceLineColor, view.ProjectionSurfaceLineThickness);

            projectionLineCam2Point1.X = (int) (projectionCenterPoint.X + Math.Cos(0.785398) * view.ProjectionCoefficent * 170) + view.ProjectionCoefficent * projectionLineCam2Bias;
            projectionLineCam2Point1.Y = (int) (projectionCenterPoint.Y + Math.Sin(0.785398) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam2Bias;
            projectionLineCam2Point2.X = (int) (projectionCenterPoint.X + Math.Cos(3.92699) * view.ProjectionCoefficent * 170) + view.ProjectionCoefficent * projectionLineCam2Bias;
            projectionLineCam2Point2.Y = (int) (projectionCenterPoint.Y + Math.Sin(3.92699) * view.ProjectionCoefficent * 170) - view.ProjectionCoefficent * projectionLineCam2Bias;
            //drawman.DrawLine(dartboardProjectionFrame, projectionLineCam2Point1, projectionLineCam2Point2, view.ProjectionSurfaceLineColor, view.ProjectionSurfaceLineThickness);

            // Draw digits
            var startAngle = 0;
            var sectorAngle = 0.314159;
            drawman.DrawString(dartboardProjectionFrame,
                               "6",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 0) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 0) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "10",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 1 + 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 1 + 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "15",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 2 + 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 2 + 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "2",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 3 + 0.05) * view.ProjectionCoefficent * 190),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 3 + 0.05) * view.ProjectionCoefficent * 190),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "17",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 4 + 0.05) * view.ProjectionCoefficent * 190),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 4 + 0.05) * view.ProjectionCoefficent * 190),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "3",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 5 + 0.05) * view.ProjectionCoefficent * 190),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 5 + 0.05) * view.ProjectionCoefficent * 190),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "19",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 6 + 0.05) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 6 + 0.05) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "7",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 7) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 7) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "16",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 8) * view.ProjectionCoefficent * 210),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 8) * view.ProjectionCoefficent * 210),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "8",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 9) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 9) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "11",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 10) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 10) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "14",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 11 - 0.05) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 11 - 0.05) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "9",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 12 - 0.05) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 12 - 0.05) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "12",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 13 - 0.05) * view.ProjectionCoefficent * 200),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 13 - 0.05) * view.ProjectionCoefficent * 200),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "5",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 14 - 0.05) * view.ProjectionCoefficent * 190),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 14 - 0.05) * view.ProjectionCoefficent * 190),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "20",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 15 - 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 15 - 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "1",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 16 - 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 16 - 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "18",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 17 - 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 17 - 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "4",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 18 - 0.05) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 18 - 0.05) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);
            drawman.DrawString(dartboardProjectionFrame,
                               "13",
                               (int) (projectionCenterPoint.X + Math.Cos(startAngle + sectorAngle * 19) * view.ProjectionCoefficent * 180),
                               (int) (projectionCenterPoint.Y + Math.Sin(startAngle + sectorAngle * 19) * view.ProjectionCoefficent * 180),
                               view.ProjectionDigitsScale,
                               view.ProjectionDigitsColor,
                               view.ProjectionDigitsThickness);

            drawman.SaveToImageBox(dartboardProjectionFrame, view.DartboardProjectionImageBox);
        }

        public void CalculateDartContour()
        {
            // workingCam.roiContourFrame = workingCam.roiFrame.Clone();
            var firstImage = workingCam.roiTrasholdFrame;
            var secondImage = workingCam.videoCapture.QueryFrame().ToImage<Gray, byte>().Not()
                                        .ThresholdBinary(new Gray(workingCam.tresholdMinSlider),
                                                         new Gray(workingCam.tresholdMaxSlider));
            secondImage.ROI = roiRectangle;
            var diffImage = secondImage.AbsDiff(firstImage);
            workingCam.roiTrasholdFrame = diffImage;

            CvInvoke.FindContours(diffImage, workingCam.allContours, workingCam.matHierarсhy, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxNone);
            //CvInvoke.DrawContours(linedFrame, contours, -1, contourColor, contourThickness, offset: new System.Drawing.Point(0, (int)RoiPosYSlider.Value));

            if (workingCam.allContours.Size <= 0)
            {
                return;
            }

            for (var i = 0; i < workingCam.allContours.Size; i++)
            {
                var contour = workingCam.allContours[i];
                var arclength = CvInvoke.ArcLength(contour, true);
                if (arclength > minContourArcLength &&
                    arclength < maxContourArcLength)
                {
                    workingCam.workingContours.Push(contour);
                }
            }

            if (workingCam.workingContours.Count != 0)
            {
                Work();
            }
        }

        private void Work()
        {
            dartboardWorkingProjectionFrame = dartboardProjectionFrame.Clone();
            var contour = workingCam.workingContours.Pop();

            contourMoments = CvInvoke.Moments(contour);
            contourCenterPoint = new Point((int) (contourMoments.M10 / contourMoments.M00), (int) workingCam.roiPosYSlider + (int) (contourMoments.M01 / contourMoments.M00));
            drawman.DrawCircle(workingCam.linedFrame, contourCenterPoint, 4, new Bgr(Color.Blue).MCvScalar, 3);

            // Find contour rectangle
            var rect = CvInvoke.MinAreaRect(contour);
            var box = CvInvoke.BoxPoints(rect);
            contourBoxPoint1 = new Point((int) box[0].X, (int) workingCam.roiPosYSlider + (int) box[0].Y);
            contourBoxPoint2 = new Point((int) box[1].X, (int) workingCam.roiPosYSlider + (int) box[1].Y);
            contourBoxPoint3 = new Point((int) box[2].X, (int) workingCam.roiPosYSlider + (int) box[2].Y);
            contourBoxPoint4 = new Point((int) box[3].X, (int) workingCam.roiPosYSlider + (int) box[3].Y);
            drawman.DrawLine(workingCam.linedFrame, contourBoxPoint1, contourBoxPoint2, view.CamContourRectColor, view.CamContourRectThickness);
            drawman.DrawLine(workingCam.linedFrame, contourBoxPoint2, contourBoxPoint3, view.CamContourRectColor, view.CamContourRectThickness);
            drawman.DrawLine(workingCam.linedFrame, contourBoxPoint3, contourBoxPoint4, view.CamContourRectColor, view.CamContourRectThickness);
            drawman.DrawLine(workingCam.linedFrame, contourBoxPoint4, contourBoxPoint1, view.CamContourRectColor, view.CamContourRectThickness);

            SetupMiddlePoints();

            CalculateSpikeLine();

            CalculateCamPoi();

            var projectionPoi = TranslateCamPoiToProjection();

            var rayPoint2 = CalculateLineThroughPoi(projectionPoi);

            CollectRay(rayPoint2);

            FindProjectionPois();

            // drawman.SaveToImageBox(dartboardWorkingProjectionFrame, view.ImageBox3);

            workingCam.allContours.Clear();
            workingCam.workingContours.Clear();
        }

        private void CollectRay(Point rayPoint)
        {
            // Save rays to collection
            if (workingCam is Cam1)
            {
                storage.SaveCam1Ray(rayPoint);
            }
            else
            {
                storage.SaveCam2Ray(rayPoint);
            }
        }

        private Point CalculateLineThroughPoi(Point projectionPoi)
        {
            // Draw line from cam through projection POI
            var rayPoint1 = workingCam is Cam1
                                ? cam1SetupPoint
                                : cam2SetupPoint;

            var rayPoint2 = projectionPoi;
            var angle = FindAngle(rayPoint1, rayPoint2);
            rayPoint2.X = (int) (rayPoint1.X + Math.Cos(angle) * 2000);
            rayPoint2.Y = (int) (rayPoint1.Y + Math.Sin(angle) * 2000);

            //drawman.DrawLine(dartboardProjectionFrame, rayPoint2, rayPoint1, view.ProjectionRayColor, view.ProjectionRayThickness);
            return rayPoint2;
        }

        private Point TranslateCamPoiToProjection()
        {
            // Translate cam surface POI to dartboard projection
            var frameWidth = workingCam.originFrame.Cols;
            var frameSemiWidth = frameWidth / 2;
            var camFovAngle = 100;
            var camFovSemiAngle = camFovAngle / 2;
            var projectionPoi = new Point();
            var projectionToCenter = new Point();
            Point camSetupPoint;
            double toCenterAngle;
            var surfacePoiToCenterDistance = FindDistance(workingCam.surfaceCenterPoint1, camPoi.GetValueOrDefault());
            var surfaceLeftToPoiDistance = FindDistance(workingCam.surfaceLeftPoint1, camPoi.GetValueOrDefault());
            var surfaceRightToPoiDistance = FindDistance(workingCam.surfaceRightPoint1, camPoi.GetValueOrDefault());
            var projectionCamToCenterDistance = frameSemiWidth / Math.Sin(Math.PI * camFovSemiAngle / 180.0) * Math.Cos(Math.PI * camFovSemiAngle / 180.0);
            var projectionCamToPoiDistance = Math.Sqrt(Math.Pow(projectionCamToCenterDistance, 2) + Math.Pow(surfacePoiToCenterDistance, 2));
            var projectionPoiToCenterDistance = Math.Sqrt(Math.Pow(projectionCamToPoiDistance, 2) - Math.Pow(projectionCamToCenterDistance, 2));
            var poiCamCenterAngle = Math.Asin(projectionPoiToCenterDistance / projectionCamToPoiDistance);

            if (workingCam is Cam1)
            {
                toCenterAngle = 0.785398;
                camSetupPoint = cam1SetupPoint;
            }
            else
            {
                toCenterAngle = 2.35619;
                camSetupPoint = cam2SetupPoint;
            }

            projectionToCenter.X = (int) (camSetupPoint.X - Math.Cos(toCenterAngle) * projectionCamToCenterDistance);
            projectionToCenter.Y = (int) (camSetupPoint.Y - Math.Sin(toCenterAngle) * projectionCamToCenterDistance);

            if (surfaceLeftToPoiDistance < surfaceRightToPoiDistance)
            {
                poiCamCenterAngle *= -1;
            }

            projectionPoi.X = (int) (camSetupPoint.X + Math.Cos(toCenterAngle + poiCamCenterAngle) * 2000);
            projectionPoi.Y = (int) (camSetupPoint.Y + Math.Sin(toCenterAngle + poiCamCenterAngle) * 2000);

            //drawman.DrawCircle(dartboardProjectionFrame, projectionToCenter, view.ProjectionPoiRadius, view.ProjectionPoiColor, view.ProjectionPoiThickness);
            //drawman.DrawCircle(dartboardProjectionFrame, projectionPoi, view.ProjectionPoiRadius, view.ProjectionPoiColor, view.ProjectionPoiThickness);
            return projectionPoi;
        }

        private void FindProjectionPois()
        {
            // Find lines intersection to find projection POI and save to collection
            if (storage.Cam1RaysCollection.Count != storage.Cam2RaysCollection.Count)
            {
                return;
            }

            var counter = storage.Cam2RaysCollection.Count;
            for (var i = 0; i < counter; i++)
            {
                var poi = FindLinesIntersection(cam1SetupPoint,
                                                storage.Cam1RaysCollection.Dequeue(),
                                                cam2SetupPoint,
                                                storage.Cam2RaysCollection.Dequeue());
                storage.SavePoi(poi);
                var anotherThrow = PrepareThrowData(poi);
                storage.SaveThrow(anotherThrow);
                drawman.DrawCircle(dartboardWorkingProjectionFrame, poi, view.PoiRadius, view.PoiColor, view.PoiThickness);
                drawman.SaveToImageBox(dartboardWorkingProjectionFrame, view.DartboardProjectionImageBox);
                view.PointsBox.Text += $"{anotherThrow.Sector} x {anotherThrow.Multiplier} = {anotherThrow.TotalPoints}\n";
            }
        }

        private Throw PrepareThrowData(Point poi)
        {
            var startRadSector = -1.41372;
            var radSectorStep = 0.314159;
            var angle = FindAngle(projectionCenterPoint, poi);
            var distance = FindDistance(projectionCenterPoint, poi);
            var sector = 0;
            var multiplier = 1;

            if (distance <= view.ProjectionCoefficent * 7)
            {
                sector = 50;
            }

            if (distance > view.ProjectionCoefficent * 7 &&
                distance <= view.ProjectionCoefficent * 17)
            {
                sector = 25;
            }

            if (distance > view.ProjectionCoefficent * 170)
            {
                sector = 0;
            }

            if (distance >= view.ProjectionCoefficent * 95 &&
                distance <= view.ProjectionCoefficent * 105)
            {
                multiplier = 3;
            }

            if (distance >= view.ProjectionCoefficent * 160 &&
                distance <= view.ProjectionCoefficent * 170)
            {
                multiplier = 2;
            }

            // Find sector
            if (angle >= -1.41372 && angle < -1.099561)
            {
                sector = 1;
            }
            else if (angle >= -1.099561 && angle < -0.785402)
            {
                sector = 18;
            }
            else if (angle >= -0.785402 && angle < -0.471243)
            {
                sector = 4;
            }
            else if (angle >= -0.471243 && angle < -0.157084)
            {
                sector = 13;
            }
            else if (angle >= -0.157084 && angle < 0.157075)
            {
                sector = 6;
            }
            else if (angle >= 0.157075 && angle < 0.471234)
            {
                sector = 10;
            }
            else if (angle >= 0.471234 && angle < 0.785393)
            {
                sector = 15;
            }
            else if (angle >= 0.785393 && angle < 1.099552)
            {
                sector = 2;
            }
            else if (angle >= 1.099552 && angle < 1.413711)
            {
                sector = 17;
            }
            else if (angle >= 1.413711 && angle < 1.72787)
            {
                sector = 3;
            }
            else if (angle >= 1.72787 && angle < 2.042029)
            {
                sector = 19;
            }
            else if (angle >= 2.042029 && angle < 2.356188)
            {
                sector = 7;
            }
            else if (angle >= 2.356188 && angle < 2.670347)
            {
                sector = 16;
            }
            else if (angle >= 2.670347 && angle < 2.984506)
            {
                sector = 8;
            }
            else if (angle >= 2.984506 && angle < 3.14159 ||
                     angle >= -3.14159 && angle < -3.29868)
            {
                sector = 11;
            }
            else if (angle >= -3.29868 && angle < -2.67036)
            {
                sector = 14;
            }
            else if (angle >= -2.67036 && angle < -2.3562)
            {
                sector = 9;
            }
            else if (angle >= -2.3562 && angle < -2.04204)
            {
                sector = 12;
            }
            else if (angle >= -2.04204 && angle < -1.72788)
            {
                sector = 5;
            }
            else if (angle >= -1.72788 && angle < -1.41372)
            {
                sector = 20;
            }

            return new Throw(poi, sector, multiplier, dartboardProjectionFrame);
        }

        private void SetupMiddlePoints()
        {
            // Setup vertical contour middlepoints 
            if (FindDistance(contourBoxPoint1, contourBoxPoint2) < FindDistance(contourBoxPoint4, contourBoxPoint1))
            {
                contourBoxMiddlePoint1 = FindMiddle(contourBoxPoint1, contourBoxPoint2);
                contourBoxMiddlePoint2 = FindMiddle(contourBoxPoint4, contourBoxPoint3);
            }
            else
            {
                contourBoxMiddlePoint1 = FindMiddle(contourBoxPoint4, contourBoxPoint1);
                contourBoxMiddlePoint2 = FindMiddle(contourBoxPoint3, contourBoxPoint2);
            }
        }

        private void CalculateCamPoi()
        {
            // Find point of impact with surface
            camPoi = FindLinesIntersection(spikeLinePoint1, spikeLinePoint2, workingCam.surfacePoint1, workingCam.surfacePoint2);
            if (camPoi != null)
            {
                drawman.DrawCircle(workingCam.linedFrame, camPoi.Value, view.ProjectionPoiRadius, view.ProjectionPoiColor, view.ProjectionPoiThickness);
            }
        }

        private void CalculateSpikeLine()
        {
            // Find spikeLine to surface
            spikeLinePoint1 = contourBoxMiddlePoint1;
            spikeLinePoint2 = contourBoxMiddlePoint2;
            workingCam.spikeLineLength = workingCam.surfacePoint2.Y - contourBoxMiddlePoint2.Y;
            var angle = FindAngle(contourBoxMiddlePoint2, contourBoxMiddlePoint1);
            spikeLinePoint1.X = (int) (contourBoxMiddlePoint2.X + Math.Cos(angle) * workingCam.spikeLineLength);
            spikeLinePoint1.Y = (int) (contourBoxMiddlePoint2.Y + Math.Sin(angle) * workingCam.spikeLineLength);
            drawman.DrawLine(workingCam.linedFrame, spikeLinePoint1, spikeLinePoint2, view.CamSpikeLineColor, view.CamSpikeLineThickness);
        }

        private Point FindLinesIntersection(Point line1Point1, Point line1Point2, Point line2Point1,
                                            Point line2Point2)
        {
            var tolerance = 0.001;
            double x1 = line1Point1.X;
            double y1 = line1Point1.Y;
            double x2 = line1Point2.X;
            double y2 = line1Point2.Y;
            double x3 = line2Point1.X;
            double y3 = line2Point1.Y;
            double x4 = line2Point2.X;
            double y4 = line2Point2.Y;
            double x;
            double y;

            if (Math.Abs(x1 - x2) < tolerance)
            {
                var m2 = (y4 - y3) / (x4 - x3);
                var c2 = -m2 * x3 + y3;
                x = x1;
                y = c2 + m2 * x1;
            }
            else if (Math.Abs(x3 - x4) < tolerance)
            {
                var m1 = (y2 - y1) / (x2 - x1);
                var c1 = -m1 * x1 + y1;
                x = x3;
                y = c1 + m1 * x3;
            }
            else
            {
                var m1 = (y2 - y1) / (x2 - x1);
                var c1 = -m1 * x1 + y1;
                var m2 = (y4 - y3) / (x4 - x3);
                var c2 = -m2 * x3 + y3;
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;
            }

            return new Point {X = (int) x, Y = (int) y};
        }

        private Point FindMiddle(Point point1, Point point2)
        {
            var mpX = (point1.X + point2.X) / 2;
            var mpY = (point1.Y + point2.Y) / 2;
            return new Point(mpX, mpY);
        }

        private int FindDistance(Point point1, Point point2)
        {
            return (int) Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
        }

        private double FindAngle(Point point1, Point point2)
        {
            return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        }

        public bool ThrowDetected()
        {
            var firstImage = workingCam.originFrame.ToUMat().ToImage<Gray, byte>();
            firstImage.ROI = roiRectangle;
            var secondImage = workingCam.videoCapture.QueryFrame().ToImage<Gray, byte>();
            secondImage.ROI = roiRectangle;
            var diffImage = secondImage.AbsDiff(firstImage);

            var moves = diffImage.CountNonzero()[0];

            // view.PointsBox.Text = $"{moves.ToString()}\n";

            if (moves > 590000)
            {
                Thread.Sleep(50);
                return true;
            }

            return false;
        }
    }
}