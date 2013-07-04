using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Media3D;
using Microsoft.VisualBasic.PowerPacks;

// for kinect
using Microsoft.Research.Kinect.Nui;
//using Microsoft.Kinect;

namespace WPFPrototype
{

    public struct calibrationStruct
    {
        //These are the calibration values that will be recorded
        public Point3D LEFTHAND_NOELBOWS_LOW;
        public Point3D LEFTHAND_ELBOWS_LOW;
        public Point3D RIGHTHAND_NOELBOWS_LOW;
        public Point3D RIGHTHAND_ELBOWS_LOW;
        public Point3D LEFTHAND_NOELBOWS_HIGH;
        public Point3D LEFTHAND_ELBOWS_HIGH;
        public Point3D RIGHTHAND_NOELBOWS_HIGH;
        public Point3D RIGHTHAND_ELBOWS_HIGH;
        public Point3D RIGHTHAND_CLOSE;
        public Point3D RIGHTHAND_FAR;


        public override string ToString()
        {
            string alldata;

            alldata = LEFTHAND_NOELBOWS_LOW.ToString() + "\r\n" + LEFTHAND_ELBOWS_LOW.ToString() +
                        "\r\n" + RIGHTHAND_NOELBOWS_LOW.ToString() + "\r\n" + RIGHTHAND_ELBOWS_LOW.ToString()
                        + "\r\n" + LEFTHAND_NOELBOWS_HIGH.ToString() + "\r\n" + LEFTHAND_ELBOWS_HIGH.ToString()
                        + "\r\n" + RIGHTHAND_NOELBOWS_HIGH.ToString() + "\r\n" + RIGHTHAND_ELBOWS_HIGH.ToString()
                        + "\r\n" + RIGHTHAND_CLOSE.ToString() + "\r\n" + RIGHTHAND_FAR.ToString();

            return alldata;

        }
    }

    /// <summary>
    /// Interaction logic for CalibrateWindow.xaml
    /// The calibration window will be used to initialize the ranges of the hand motions which will be used in the Gesture window in order
    /// to produce the music. (See Logs, and Planning Files for more Information)
    /// </summary>
    public partial class CalibrateWindow : Window
    {
        bool calibrateComplete = false;
        bool calibrationStart = false;
        Runtime kinect_calibrate;
        DispatcherTimer dispatcherTimer;
        int calibrateIndex = 0; //0 through 9 correspond to the joints
        calibrationStruct JOINT_CALIBRATION_VALUES = new calibrationStruct();
        SkeletonFrame skeletonFrame;
        int screenY;
        string imageAddressRight = "D:\\Sai\\Sai Thesis\\saiThesisResearch\\Kinect code\\_StudyProgram\\handImageRight.png";
        string imageAddressLeft = "D:\\Sai\\Sai Thesis\\saiThesisResearch\\Kinect code\\_StudyProgram\\handImageLeft.png";
        string imageAddressRightCloser = "D:\\Sai\\Sai Thesis\\saiThesisResearch\\Kinect code\\_StudyProgram\\handImageRight_CLOSER.png";
        string imageAddressRightFurther = "D:\\Sai\\Sai Thesis\\saiThesisResearch\\Kinect code\\_StudyProgram\\handImageRight_FURTHER.png";
        string giftest = "D:\\Sai\\Sai Thesis\\saiThesisResearch\\Kinect code\\_StudyProgram\\giftest.gif";

        public CalibrateWindow()
        {
            InitializeComponent();
        }
        
        Dictionary<JointID, Brush> jointColors = new Dictionary<JointID, Brush>() { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };

        private void GestureWindowButton_Click(object sender, RoutedEventArgs e)
        {
            kinect_calibrate.Uninitialize();
            GestureWindow gestureWindowForm = new GestureWindow();
            gestureWindowForm.initializeRanges(JOINT_CALIBRATION_VALUES);
            gestureWindowForm.Show();

           // StudyWindow StudyWindowForm = new StudyWindow();
            //StudyWindowForm.initializeRanges(JOINT_CALIBRATION_VALUES);
            //StudyWindowForm.Show();

            this.Close();

        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            //Loads a calibration file then enables gesturewindowbutton
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text Documents (.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;

                System.IO.StreamReader file =
                   new System.IO.StreamReader(filename);

                string filetext = System.IO.File.ReadAllText(filename);

                loadCalibrationJointValues(filetext, filename);

            }
            
        }

        public void loadCalibrationJointValues(string filetext, string filename)
        {
            //parse the text file. 
            //try to initialize all variables, if it doesnt work, then throw error message

            char[] delimiterChars = {',', '\t', '\r', '\n'};
            string[] words = filetext.Split(delimiterChars);

            try
            {

                //alldata = LEFTHAND_NOELBOWS_LOW.ToString() + "\r\n" + LEFTHAND_ELBOWS_LOW.ToString() +
                //            "\r\n" + RIGHTHAND_NOELBOWS_LOW.ToString() + "\r\n" + RIGHTHAND_ELBOWS_LOW.ToString()
                //            + "\r\n" + LEFTHAND_NOELBOWS_HIGH.ToString() + "\r\n" + LEFTHAND_ELBOWS_HIGH.ToString()
                //            + "\r\n" + RIGHTHAND_NOELBOWS_HIGH.ToString() + "\r\n" + RIGHTHAND_ELBOWS_HIGH.ToString()
                //            + "\r\n" + RIGHTHAND_CLOSE.ToString() + "\r\n" + RIGHTHAND_FAR.ToString();

                //MODIFY THE FILE LOADING PROGRAM IN THE FUTURE, IT IS VERY PRIMITIVE AS IT IS NOW. 

                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.X = double.Parse(words[0]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.Y = double.Parse(words[1]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.Z = double.Parse(words[2]);

                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.X = double.Parse(words[4]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.Y = double.Parse(words[5]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.Z = double.Parse(words[6]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.X = double.Parse(words[8]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.Y = double.Parse(words[9]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.Z = double.Parse(words[10]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.X = double.Parse(words[12]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.Y = double.Parse(words[13]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.Z = double.Parse(words[14]);

                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.X = double.Parse(words[16]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.Y = double.Parse(words[17]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.Z = double.Parse(words[18]);

                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.X = double.Parse(words[20]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.Y = double.Parse(words[21]);
                JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.Z = double.Parse(words[22]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.X = double.Parse(words[24]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.Y = double.Parse(words[25]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.Z = double.Parse(words[26]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.X = double.Parse(words[28]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.Y = double.Parse(words[29]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.Z = double.Parse(words[30]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.X = double.Parse(words[32]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.Y = double.Parse(words[33]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.Z = double.Parse(words[34]);

                JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.X = double.Parse(words[36]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.Y = double.Parse(words[37]);
                JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.Z = double.Parse(words[38]);

                feedbackLabel.Content = "File loaded successfully. Calibration Complete. \nClick \"Proceed\" to continue";
                GestureWindowButton.IsEnabled = true;
            }
            catch (InvalidCastException e)
            {
                System.Windows.MessageBox.Show("The selected file: is not a valid Calibration save file!", "Error", MessageBoxButton.OK ,MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //Saves current calibration (once calibration is complete)
            Microsoft.Win32.SaveFileDialog SaveDialog = new Microsoft.Win32.SaveFileDialog();
            SaveDialog.FileName = "Default_Calibration";
            SaveDialog.DefaultExt = ".text";
            SaveDialog.Filter = "Text Documents (.txt)|*.txt";

            if (SaveDialog.ShowDialog() == true)
            {
                //generate data using the skeleton data, then save to file specified
                string fileText;
                fileText = JOINT_CALIBRATION_VALUES.ToString();
                File.WriteAllText(SaveDialog.FileName, fileText);
            }
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            kinect_calibrate.Uninitialize();
            this.Close();

        }

        private void startCalibration_Click(object sender, RoutedEventArgs e)
        {
            //This button starts the calibration sequence
            //the joints will be calibrated in the following order:
            /*
             * 1. Left Hand - Highest Reach (elbows relaxed)
             * 2. Left Hand - Lowest Reach  (elbows relaxed)
             * 3. Left Hand - Furthest Reach
             * 4. Left Hand - Lowest Reach
             * 5-8. Right Hand.
             * 9. Right Hand - Hand flat near the chest (furthest distance from camera)
             * 10. Right Hand - Hand extended towards teh camera. (closest distance from the camera)
             */
            //A Timer will be started which will take approximately 2 seconds to get the data for each joint. 
            //The timed loop will switch between joints automatically

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();
            startCalibration.IsEnabled = false;
            feedbackLabel.Content = "Please follow the hand on the screen...";
            SaveDialogButton.IsEnabled = false;
            calibrationStart = true;
            GestureWindowButton.IsEnabled = true;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //Timer reached a tick. perform code.

            feedbackLabel.Content = "Please follow the hand on the screen...";

            switch (calibrateIndex)
            {
                case 0:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 1:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 2:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 3:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 4:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 5:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 6:
                    {
                        calibrateIndex++;
                        break;
                    }
                case 7:
                    {
                        feedbackLabel.Content = " Measuring Right Hand closest Position from Kinect in 5 sec..";
                        calibrateIndex++;
                        break;
                    }
                case 8:
                    {
                        feedbackLabel.Content = "Measuring Right Hand Furthest Position from Kinect in 5 sec..";
                        calibrateIndex++;
                        break;
                    }
                case 9:
                    {
                        feedbackLabel.Content = "Calibration Complete.";
                        SaveDialogButton.IsEnabled = true;
                        calibrateIndex++;
                        calibrationStart = false;
                        startCalibration.IsEnabled = true;
                        GestureWindowButton.IsEnabled = true;
                        break;
                    }
                default: break;
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            //set up screenY variable needed for joint screen coordinates
            screenY = (int) skeleton.Height;

            // start the kinect
            kinect_calibrate = new Runtime();

            // initalise for depth camera and skeletal tracking
            try
            {

                kinect_calibrate.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseSkeletalTracking);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }

            // open stream
            try
            {
                kinect_calibrate.VideoStream.Open(ImageStreamType.Video, 1, ImageResolution.Resolution640x480, ImageType.Color);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }


            // set up the event handler
            kinect_calibrate.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            kinect_calibrate.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(kinect_ColorFrameReady);
        }


        void kinect_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            PlanarImage Image = e.ImageFrame.Image;
            video.Source = BitmapSource.Create(
                Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, Image.Bits, Image.Width * Image.BytesPerPixel);
        }

        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            kinect_calibrate.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = depthX * 320; //convert to 320, 240 space
            depthY = depthY * 240; //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            kinect_calibrate.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
        }

        Polyline getBodySegment(Microsoft.Research.Kinect.Nui.JointsCollection joints, Brush brush, params JointID[] ids)
        {
            PointCollection points = new PointCollection(ids.Length);
            for (int i = 0; i < ids.Length; ++i)
            {
                points.Add(getDisplayPosition(joints[ids[i]]));
            }

            Polyline polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
        }

        Point getJointLocation(Microsoft.Research.Kinect.Nui.JointsCollection joints, JointID ids)
        {
            Point a = new Point();

            a = (getDisplayPosition(joints[ids]));

            return a;
        }

        void drawHandOnCanvas(Canvas skeleton, int hand, Point handPos, int width, bool scaleAndTransparant, double scale, double opaque)
        {                                    
            // Create Image Element
            Image handImage = new Image();
            // Create source
            BitmapImage myBitmapImage = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            myBitmapImage.BeginInit();
            if (hand == 0)
            {
                myBitmapImage.UriSource = new Uri(imageAddressLeft);
                myBitmapImage.DecodePixelWidth = width;
                myBitmapImage.EndInit();
                //set image source
                handImage.Source = myBitmapImage;
                Canvas.SetTop(handImage, handPos.Y);
                Canvas.SetLeft(handImage, handPos.X);
                skeleton.Children.Add(handImage);
            }
            else if (hand == 1)
            {
                myBitmapImage.UriSource = new Uri(imageAddressRight);
                myBitmapImage.DecodePixelWidth = width;
                myBitmapImage.EndInit();
                //set image source
                handImage.Source = myBitmapImage;
                Canvas.SetTop(handImage, handPos.Y);
                Canvas.SetLeft(handImage, handPos.X - 30);//offsetting by 30 pixels because the right hand seems to be shown further away than the left hand by 30 pixels.
                skeleton.Children.Add(handImage);
            }
            else if (hand == 3)
            {
                //draw close to the kinect
                myBitmapImage.UriSource = new Uri(imageAddressRightCloser);
                myBitmapImage.DecodePixelWidth = (int)(width * scale);
                myBitmapImage.EndInit();
                //set image source
                handImage.Source = myBitmapImage;
                //handImage.Opacity = opaque;
                Canvas.SetTop(handImage, handPos.Y);
                Canvas.SetLeft(handImage, handPos.X - 30);//offsetting by 30 pixels because the right hand seems to be shown further away than the left hand by 30 pixels.
                skeleton.Children.Add(handImage);
            }
            else if (hand == 4)
            {
                //draw close to the body
                myBitmapImage.UriSource = new Uri(imageAddressRightFurther);
                myBitmapImage.DecodePixelWidth =  (int)(width * scale);
                myBitmapImage.EndInit();
                //set image source
                handImage.Source = myBitmapImage;
                //handImage.Opacity = opaque;
                Canvas.SetTop(handImage, handPos.Y);
                Canvas.SetLeft(handImage, handPos.X - 30);//offsetting by 30 pixels because the right hand seems to be shown further away than the left hand by 30 pixels.
                skeleton.Children.Add(handImage);
            }
        }



        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            skeletonFrame = e.SkeletonFrame;
            int iSkeleton = 0;
            Brush[] brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));



            skeleton.Children.Clear();

            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {

                if (SkeletonTrackingState.Tracked == data.TrackingState)            
                {

                    if (calibrationStart)
                    {
                        Point temp;
                        //initialize variables here
                        //However, X, and Y Variables are based off the screen position instead of the 3-d Location
                        //Z variable is based on the 3-d Joint position.

                        switch (calibrateIndex)
                        {
                            case 0:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.Spine);

                                    Point handPos = new Point(a.X - x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 1, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand


                                    temp = getJointLocation(data.Joints, JointID.HandLeft);
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_LOW.Z = data.Joints[JointID.HandLeft].Position.Z;

                                    break;
                                }
                            case 1:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.KneeLeft);

                                    Point handPos = new Point(a.X - x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 1, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandLeft);
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_LOW.Z = data.Joints[JointID.HandLeft].Position.Z;
                                    break;
                                }
                            case 2:
                                {
                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.Spine);

                                    Point handPos = new Point(b.X + x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 0, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_LOW.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            case 3:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.KneeRight);


                                    Point handPos = new Point(b.X + x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 0, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_LOW.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            case 4:
                                {


                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);

                                    Point handPos = new Point(a.X - x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 1, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandLeft);
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_NOELBOWS_HIGH.Z = data.Joints[JointID.HandLeft].Position.Z;
                                    break;
                                }
                            case 5:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);
                                    Point d = getJointLocation(data.Joints, JointID.Head);
                                    double y_adj = Math.Abs(d.Y - c.Y);

                                    Point handPos = new Point(a.X - x_adj, d.Y - (y_adj));
                                    drawHandOnCanvas(skeleton, 1, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandLeft);
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.LEFTHAND_ELBOWS_HIGH.Z = data.Joints[JointID.HandLeft].Position.Z;
                                    break;
                                }
                            case 6:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);

                                    Point handPos = new Point(b.X + x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 0, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_NOELBOWS_HIGH.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            case 7:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);
                                    Point d = getJointLocation(data.Joints, JointID.Head);
                                    double y_adj = Math.Abs(d.Y - c.Y);

                                    Point handPos = new Point(b.X + x_adj, d.Y - y_adj);
                                    drawHandOnCanvas(skeleton, 0, handPos, 30, false, 0, 0); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_ELBOWS_HIGH.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            case 8:
                                {
                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);

                                    Point handPos = new Point((skeleton.Width) / 2 - 75, 0);//(skeleton.Height / 2) - 75);
                                    drawHandOnCanvas(skeleton, 3, handPos, 30, true, 10, 0.5); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_CLOSE.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            case 9:
                                {

                                    Point a = getJointLocation(data.Joints, JointID.ShoulderLeft);
                                    Point b = getJointLocation(data.Joints, JointID.ShoulderRight);
                                    double x_adj = Math.Abs(b.X - a.X); //This will be used to adjust the x-coordinate of the hand

                                    Point c = getJointLocation(data.Joints, JointID.ShoulderCenter);

                                    //fix magix number 75
                                    Point handPos = new Point((skeleton.Width) / 2 - 75, 0);//(skeleton.Height / 2) - 75);
                                    //Point handPos = new Point(b.X + x_adj, c.Y);
                                    drawHandOnCanvas(skeleton, 4, handPos, 30, true, 10, 0.5); //0 is right hand, 1 is left hand

                                    temp = getJointLocation(data.Joints, JointID.HandRight);
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.X = temp.X;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.Y = screenY - temp.Y;
                                    JOINT_CALIBRATION_VALUES.RIGHTHAND_FAR.Z = data.Joints[JointID.HandRight].Position.Z;
                                    break;
                                }
                            default: break;
                        }
                    }

                    // Draw bones
                    Brush brush = brushes[iSkeleton % brushes.Length];
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));

                    // Draw joints
                    foreach (Joint joint in data.Joints)
                    {
                        Point jointPos = getDisplayPosition(joint);
                        Line jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = jointColors[joint.ID];
                        jointLine.StrokeThickness = 6;
                        skeleton.Children.Add(jointLine);
                    }                    
                }
                iSkeleton++;
            } // for each skeleton

            if (calibrateComplete)
                GestureWindowButton.IsEnabled = true;
        }

        private void cancelCalibrate_Click(object sender, RoutedEventArgs e)
        {
            //Cancel's the timer, and renable's the Begin button. 
            startCalibration.IsEnabled = true;
            dispatcherTimer.Stop();
            calibrateComplete = false;
            calibrationStart = false;
            calibrateIndex = 0;
        }

    }
}
