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

// for kinect
using Microsoft.Research.Kinect.Nui;
//using Microsoft.Kinect;

//for osc
using Ventuz.OSC;
using System.IO;
using System.Windows.Media.Media3D;

namespace WPFPrototype
{
    /// <summary>
    /// Interaction logic for GestureWindow.xaml
    /// Ideally the Gesture window will use the data provided by the calibrate window to initialize the hand ranges and start the 
    /// actual program
    /// </summary>
    /// 
    public struct WindowBounds
    {
        double LowerBound;
        double UpperBound;
        double Strength;

        public double getLowerBound()
        {
            return LowerBound;
        }
        public void setLowerBound(double lb)
        {
            LowerBound = lb;
        }

        public double getUpperBound()
        {
            return UpperBound;
        }
        public void setUpperBound(double hb)
        {
            UpperBound = hb;
        }
        public void setStrength(double st)
        {
            Strength = st;

        }
        public double getStrength()
        {
                return Strength;
        }
        public void adjustStrength(double st)
        {
            Strength -= st;
            if (Strength < 0)
            {
                Strength = 0;
            }
        }
    }

    public partial class GestureWindow : Window
    {
        public GestureWindow()
        {
            InitializeComponent();
        }

        Runtime kinect;
        const int frequencyZero = 0;
        const int frequencyLow = 30;
        const int frequencyMid = 270;
        const int frequencyMax = 300;
        int rangeMid;
        int rangeHigh;
        int rangeLow;
        const int frequencyRangeLow = 30;
        const int frequencyRangeMid = 240;
        const int frequencyRangeHigh = 30;
        double ratioLow;
        double ratioMid;
        double ratioHigh;
        const int amplitudeMax = 157;
        const int amplitudeZero = 0;
        int rhClose;
        int rhFar;
        int rangeAmplitude;
        double ratioAmplitude;
        int screenX;
        int screenY;
        const int zScale = 1000;
        const int NUMBER_OF_CHANNELS = 8;
        double amplitudeWindowSize;
        const double amplitudeWindowPercentage = 1.2;
        double[] channelAmplitudes; //0-7 correspond to channels 1-8
        double amplitudeWindowHIGH;
        double amplitudeWindowLOW;
        double modifiedWindowSize;
        double amplitudeWindowSize_Half;
        Point amplitudeChannels; //x stores low range, y stores high
        double[] amplitudeChannelHigh;//stores the highest positions from channel 1-8. (0-7)
        double[] amplitudeChannelLow;//stores the lowest positions from channel 1-8. (0-7)

        DispatcherTimer recordTimer;
        Boolean record = false;
        string recordFilename;
        string vtcFilename;
        StreamWriter recordLog;
        StreamWriter vtcLog;
        int timestamp; //will be used to keep track of time elapsed
        string recordTag;

        DispatcherTimer Muter;
        Boolean mute = false;
        int stimulusPlayBackTimeSpan = 0;

        DispatcherTimer countdownTimer;
        int countdown = 3; //starts at 3,2,1..

        calibrationStruct calibrationData;
        string testfilename = "C:\\Users\\clt\\Desktop\\testoutputfile.txt";
        string allteststring;

        Boolean finalAttempt = false;
        Image video = new Image();

        //This timer will tick 10 times per second polling for the user's hand positions
        //it grabs these positions and sends them over to Michael Pouris' music visualization program, using the port: 6771
        DispatcherTimer MusicVizHandler;
        int MusicVizPort = 6771;
        int MidiMax = 12;

        //variables that will be used for the study
        // a new udp stream, for a new 
        //private UdpWriter OSCStudyOut = new UdpWriter("127.0.0.1", 6601);//used to communicate with a max patch which is responsible for playing the stimuli
        //private UdpReader OSCStudyIn = new UdpReader(6606, "127.0.0.1"); //max patch will reply saying stimuli has been played, which resumes normal functionality of the program.
        //more on how this works in the log file. (Jan 11)
        int currentStimulus = -3; //starts at -3 for 4 practice cases
        int totalstimuli = 17;
        VariableTestCase vtc;
        DispatcherTimer vtcTimer;
        int vtcCounter = 0; //increments by 100 each time, representing 100 ms

        double freqOut;
        double freqOut2;
        //set up writer
        private UdpWriter OSCSender = new UdpWriter("127.0.0.1", 6601);
        private UdpWriter MusicVizSender = new UdpWriter("127.0.0.1", 6771);
        int saveStateNote;

        int replaycount = 0;

        /*new window variables
         * midwindowsize and neighbouring window size will be set when the channel amplitude is calculated. 
         * they will be the same size for simplicity
         * 
         */
        int MidWindowSize;
        int NeighborWindowSize;
        const int MidWindowStrength = 80;
        const int NeighbourindWindowStrength = 10;
        static WindowBounds[] windowBounds = new WindowBounds[3];
        static WindowBounds[] channelBounds = new WindowBounds[8];

        /*
         * FUNCTIONS ARE BELOW
         */
        

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


        public void initializeRanges(calibrationStruct calib)
        {
            //Initialize the calibration structure here
            calibrationData.LEFTHAND_NOELBOWS_LOW.X = calib.LEFTHAND_NOELBOWS_LOW.X;
            calibrationData.LEFTHAND_NOELBOWS_LOW.Y = calib.LEFTHAND_NOELBOWS_LOW.Y;
            calibrationData.LEFTHAND_NOELBOWS_LOW.Z = calib.LEFTHAND_NOELBOWS_LOW.Z;

            calibrationData.LEFTHAND_ELBOWS_LOW.X = calib.LEFTHAND_ELBOWS_LOW.X;
            calibrationData.LEFTHAND_ELBOWS_LOW.Y = calib.LEFTHAND_ELBOWS_LOW.Y;
            calibrationData.LEFTHAND_ELBOWS_LOW.Z = calib.LEFTHAND_ELBOWS_LOW.Z;

            calibrationData.RIGHTHAND_NOELBOWS_LOW.X = calib.RIGHTHAND_NOELBOWS_LOW.X;
            calibrationData.RIGHTHAND_NOELBOWS_LOW.Y = calib.RIGHTHAND_NOELBOWS_LOW.Y;
            calibrationData.RIGHTHAND_NOELBOWS_LOW.Z = calib.RIGHTHAND_NOELBOWS_LOW.Z;

            calibrationData.RIGHTHAND_ELBOWS_LOW.X = calib.RIGHTHAND_ELBOWS_LOW.X;
            calibrationData.RIGHTHAND_ELBOWS_LOW.Y = calib.RIGHTHAND_ELBOWS_LOW.Y;
            calibrationData.RIGHTHAND_ELBOWS_LOW.Z = calib.RIGHTHAND_ELBOWS_LOW.Z;

            calibrationData.LEFTHAND_NOELBOWS_HIGH.X = calib.LEFTHAND_NOELBOWS_HIGH.X;
            calibrationData.LEFTHAND_NOELBOWS_HIGH.Y = calib.LEFTHAND_NOELBOWS_HIGH.Y;
            calibrationData.LEFTHAND_NOELBOWS_HIGH.Z = calib.LEFTHAND_NOELBOWS_HIGH.Z;

            calibrationData.LEFTHAND_ELBOWS_HIGH.X = calib.LEFTHAND_ELBOWS_HIGH.X;
            calibrationData.LEFTHAND_ELBOWS_HIGH.Y = calib.LEFTHAND_ELBOWS_HIGH.Y;
            calibrationData.LEFTHAND_ELBOWS_HIGH.Z = calib.LEFTHAND_ELBOWS_HIGH.Z;

            calibrationData.RIGHTHAND_NOELBOWS_HIGH.X = calib.RIGHTHAND_NOELBOWS_HIGH.X;
            calibrationData.RIGHTHAND_NOELBOWS_HIGH.Y = calib.RIGHTHAND_NOELBOWS_HIGH.Y;
            calibrationData.RIGHTHAND_NOELBOWS_HIGH.Z = calib.RIGHTHAND_NOELBOWS_HIGH.Z;

            calibrationData.RIGHTHAND_ELBOWS_HIGH.X = calib.RIGHTHAND_ELBOWS_HIGH.X;
            calibrationData.RIGHTHAND_ELBOWS_HIGH.Y = calib.RIGHTHAND_ELBOWS_HIGH.Y;
            calibrationData.RIGHTHAND_ELBOWS_HIGH.Z = calib.RIGHTHAND_ELBOWS_HIGH.Z;

            calibrationData.RIGHTHAND_CLOSE.X = calib.RIGHTHAND_CLOSE.X;
            calibrationData.RIGHTHAND_CLOSE.Y = calib.RIGHTHAND_CLOSE.Y;
            calibrationData.RIGHTHAND_CLOSE.Z = calib.RIGHTHAND_CLOSE.Z;

            calibrationData.RIGHTHAND_FAR.X = calib.RIGHTHAND_FAR.X;
            calibrationData.RIGHTHAND_FAR.Y = calib.RIGHTHAND_FAR.Y;
            calibrationData.RIGHTHAND_FAR.Z = calib.RIGHTHAND_FAR.Z;

            //call the function which converts and normalizes the ranges
            //from here on out the program will only use the normalized values by pulling from a table or formula.
            rangeMid = (int)(calib.LEFTHAND_NOELBOWS_HIGH.Y - calib.LEFTHAND_NOELBOWS_LOW.Y);
            rangeLow = (int)(calib.LEFTHAND_NOELBOWS_LOW.Y - calib.LEFTHAND_ELBOWS_LOW.Y);
            rangeHigh = (int)(calib.LEFTHAND_ELBOWS_HIGH.Y - calib.LEFTHAND_NOELBOWS_HIGH.Y);

            ratioLow = (double)frequencyRangeLow / (double)rangeLow;
            ratioMid = (double)frequencyRangeMid / (double)rangeMid;
            ratioHigh = (double)frequencyRangeHigh / (double)rangeHigh;

            //for simplicity sake, i will scale right hand as 1 zone. then i will combine the zones to add detail. 
            rangeAmplitude = (int)(calib.RIGHTHAND_ELBOWS_HIGH.Y - calib.RIGHTHAND_ELBOWS_LOW.Y);
            ratioAmplitude = amplitudeMax / rangeAmplitude;

            rhClose = (int)(zScale * calibrationData.RIGHTHAND_CLOSE.Z);
            rhFar = (int)(zScale * calibrationData.RIGHTHAND_FAR.Z);

            amplitudeWindowSize = (calibrationData.RIGHTHAND_ELBOWS_HIGH.Y - calibrationData.RIGHTHAND_ELBOWS_LOW.Y) / NUMBER_OF_CHANNELS;
            modifiedWindowSize = amplitudeWindowSize * amplitudeWindowPercentage;
            channelAmplitudes = new double[NUMBER_OF_CHANNELS];
            amplitudeChannelHigh = new double[NUMBER_OF_CHANNELS];
            amplitudeChannelLow = new double[NUMBER_OF_CHANNELS];

            System.Console.WriteLine("rangeMid " + rangeMid);
            System.Console.WriteLine("rangeLow " + rangeLow);
            System.Console.WriteLine("rangeHigh " + rangeHigh);

            System.Console.WriteLine("ratioLow " + ratioLow);
            System.Console.WriteLine("ratioMid " + ratioMid);
            System.Console.WriteLine("ratioHigh " + ratioHigh);

            System.Console.WriteLine("rangeAmplitude " + rangeAmplitude);
            System.Console.WriteLine("ratioAmplitude " + ratioAmplitude);

            System.Console.WriteLine("elbows Low " + calibrationData.LEFTHAND_ELBOWS_LOW.Y);
            System.Console.WriteLine("No elbows Low " + calibrationData.LEFTHAND_NOELBOWS_LOW.Y);

            System.Console.WriteLine("amplitudeWindowSize " + amplitudeWindowSize);

            double tempWindowLocation = amplitudeWindowSize + calibrationData.RIGHTHAND_ELBOWS_LOW.Y;
            for (int i = 0; i <= 7; i++)
            {

                channelBounds[i].setUpperBound((int)tempWindowLocation);
                //amplitudeChannelHigh[i] = tempWindowLocation;
                tempWindowLocation += amplitudeWindowSize;

                System.Console.WriteLine("amplitudeChannelHigh[" + i + "] " +channelBounds[i].getUpperBound());
            }

            tempWindowLocation = calibrationData.RIGHTHAND_ELBOWS_LOW.Y;
            for (int i = 0; i <= 7; i++)
            {

                channelBounds[i].setLowerBound((int)tempWindowLocation);
                //amplitudeChannelLow[i] = tempWindowLocation;
                tempWindowLocation += amplitudeWindowSize;

                System.Console.WriteLine("amplitudeChannelLow[" + i + "] " + channelBounds[i].getLowerBound());
            }

            NeighborWindowSize = (int) amplitudeWindowSize;
            MidWindowSize = (int)amplitudeWindowSize;
        }


        void recordTimer_Tick(object sender, EventArgs e)
        {
            timestamp++;
        }


        void countdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            if (countdown == 0)
            {
                countdownTimer.Stop();
            }


        }
        

        //void MusicVizHandler_Tick(object sender, EventArgs e)
       // {
            /*1. get the positions of the left hand, and right hand
             * 2. send them to MusicVizHandler (it works the same way as the OSC handler)
             * 3. use the results of the function to send the results into MusicVizSender(v1,v2,v3)
             *     music viz sender will package everything and send to the port. 
             */

           // getting freqout = pitch
            //Amplitude = velocity (4 channels)
            //Channel (4) 
            //Note = pitch.

//            double MusicVizNote = (double)(freqOut2 * MidiMax) / (double)frequencyMax;
            //System.Console.Out.WriteLine(MusicVizNote);

            //int[] MusicVizChannels = new int[4];
            //MusicVizChannels[0] = 0;
            //MusicVizChannels[1] = 1;
           // MusicVizChannels[2] = 2;
           // MusicVizChannels[3] = 3;

           // double[] MusicVizVelocity = new double[4];
          //  MusicVizVelocity[0] = (channelAmplitudes[0] + channelAmplitudes[1]) * MidiMax / amplitudeMax;
          //  MusicVizVelocity[1] = (channelAmplitudes[2] + channelAmplitudes[3]) * MidiMax / amplitudeMax;
          //  MusicVizVelocity[2] = (channelAmplitudes[4] + channelAmplitudes[5]) * MidiMax / amplitudeMax;
        //    MusicVizVelocity[3] = (channelAmplitudes[6] + channelAmplitudes[7]) * MidiMax / amplitudeMax;
        
        /**
            //send osc information to port 6771
            //praparing to send UDP message
            OscBundle bundle = new OscBundle();

            OscMessage message;

            for (int i = 0; i <= 3; i++)
            {
                //sending note off
                message = new OscElement("/channelNoteVolume", (int)MusicVizChannels[i], saveStateNote, 0);
                bundle.AddElement(message);
            }

            for (int i = 0; i <= 3; i++)
            {
                message = new OscElement("/channelNoteVolume", (int)MusicVizChannels[i], (int)MusicVizNote, (int) MusicVizVelocity[i]);
                bundle.AddElement(message);
            }

            channel1_Label.FontSize = 20;
            channel2_Label.FontSize = 20;
            channel3_Label.FontSize = 20;
            channel4_Label.FontSize = 20;

            channel1_Label.Content = "Ch0: 0 " + (int)MusicVizNote + " " + (int)MusicVizVelocity[0];
            channel2_Label.Content = "Ch0: 1 " + (int)MusicVizNote + " " + (int)MusicVizVelocity[1];
            channel3_Label.Content = "Ch0: 2 " + (int)MusicVizNote + " " + (int)MusicVizVelocity[2];
            channel4_Label.Content = "Ch0: 3 " + (int)MusicVizNote + " " + (int)MusicVizVelocity[3];

            saveStateNote = (int)MusicVizNote;
            MusicVizSender.Send(bundle);
        }

**/
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            //getfilename
            //start dumping x,y,z values into the file

            ////Saves current calibration (once calibration is complete)
            //Microsoft.Win32.SaveFileDialog SaveDialog = new Microsoft.Win32.SaveFileDialog();
            //SaveDialog.Title = "Save Participant Record File";
            //SaveDialog.FileName = "subject";
            //SaveDialog.DefaultExt = "";
            //SaveDialog.Filter = "";

            //if (SaveDialog.ShowDialog() == true)
            //{
            //    //generate data using the skeleton data, then save to file specified
            //    recordFilename = SaveDialog.FileName.ToString();
            //    recordLog = new StreamWriter(recordFilename);
            //}

            recordTimer = new DispatcherTimer();
            recordTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            recordTimer.Tick += new EventHandler(recordTimer_Tick);

            
            saveStateNote = 0;
           /** 
            MusicVizHandler = new DispatcherTimer();
            MusicVizHandler.Interval = new TimeSpan(0, 0, 0, 0, 300);
            MusicVizHandler.Tick += new EventHandler(MusicVizHandler_Tick);
            MusicVizHandler.Start();
            **/
            //initialize
            screenY = (int)skeleton.Height;
            screenX = (int)skeleton.Width;
            // start the kinect
            kinect = new Runtime();

            // initalise for depth camera and skeletal tracking
            try
            {
                kinect.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseSkeletalTracking);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }

            // open stream
            try
            {
                kinect.VideoStream.Open(ImageStreamType.Video, 1, ImageResolution.Resolution640x480, ImageType.Color);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }

            video.Height = 375;
            video.Width = 500;
            video.Stretch = Stretch.Fill;
            video.VerticalAlignment = VerticalAlignment.Top;

            // set up the event handler
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
            kinect.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(kinect_ColorFrameReady);

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = new TimeSpan(0, 0, 0, (int)(vtc.timedelay) / 3);
            countdownTimer.Tick += new EventHandler(countdownTimer_Tick);

            //BackButton.IsEnabled = false;
            //ReplayStimulusButton.IsEnabled = false;
        }


        void kinect_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            PlanarImage Image = e.ImageFrame.Image;
            video.Source = BitmapSource.Create(
                Image.Width, Image.Height,96, 96, PixelFormats.Bgr32, null, Image.Bits, Image.Width * Image.BytesPerPixel);
            


            //System.Console.Out.WriteLine("Height: " + video.ActualHeight + "Width: " + video.ActualWidth);
        }

        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            kinect.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = depthX * 320; //convert to 320, 240 space
            depthY = depthY * 240; //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            kinect.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

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


        int frequencyOUTCalculator(Point Left_Hand, Point Right_Hand)
        {
            int frequencyOUT = 0;
            if ((Left_Hand.Y > calibrationData.LEFTHAND_ELBOWS_LOW.Y) && (Left_Hand.Y < calibrationData.LEFTHAND_NOELBOWS_LOW.Y))
            {

                double A = (Left_Hand.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y) /
                            (calibrationData.LEFTHAND_NOELBOWS_LOW.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y);
                //double B = frequency range of the zone. (frequencyLOW)
                //double C = Max freq of the zone below. for Lowest zone it is Zero. 

                frequencyOUT = (int)(A * frequencyLow) + 0;
            }
            else if ((Left_Hand.Y > calibrationData.LEFTHAND_NOELBOWS_LOW.Y) && (Left_Hand.Y < calibrationData.LEFTHAND_NOELBOWS_HIGH.Y))
            {
                double A = (Left_Hand.Y - calibrationData.LEFTHAND_NOELBOWS_LOW.Y) /
                            (calibrationData.LEFTHAND_NOELBOWS_HIGH.Y - calibrationData.LEFTHAND_NOELBOWS_LOW.Y);
                //double B = frequency range of the zone. (frequencyLOW)
                //double C = Max freq of the zone below. for Lowest zone it is Zero. 

                frequencyOUT = (int)(A * frequencyMid) + frequencyLow;
            }
            else if ((Left_Hand.Y > calibrationData.LEFTHAND_NOELBOWS_HIGH.Y) && (Left_Hand.Y < calibrationData.LEFTHAND_ELBOWS_HIGH.Y))
            {
                double A = (Left_Hand.Y - calibrationData.LEFTHAND_NOELBOWS_HIGH.Y) /
                            (calibrationData.LEFTHAND_ELBOWS_HIGH.Y - calibrationData.LEFTHAND_NOELBOWS_HIGH.Y);
                //double B = frequency range of the zone. (frequencyLOW)
                //double C = Max freq of the zone below. for Lowest zone it is Zero. 


                //frequency low is the same range as frequency high. so i used the same variable
                frequencyOUT = (int)(A * frequencyLow) + frequencyMid;
            }


            //if ((Left_Hand.Y > calibrationData.LEFTHAND_ELBOWS_LOW.Y) && (Left_Hand.Y < calibrationData.LEFTHAND_ELBOWS_HIGH.Y))
            //{


            //    double A  = (Left_Hand.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y) / 
            //        (calibrationData.LEFTHAND_ELBOWS_HIGH.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y);

            //    //double A = (Left_Hand.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y) /
            //    //            (calibrationData.LEFTHAND_NOELBOWS_LOW.Y - calibrationData.LEFTHAND_ELBOWS_LOW.Y);
            //    //double B = frequency range of the zone. (frequencyLOW)
            //    //double C = Max freq of the zone below. for Lowest zone it is Zero. 

            //    frequencyOUT = (int)(A * frequencyMax);
            //}


            freqOut = frequencyOUT;
            freqOut2 = frequencyOUT;
            return frequencyOUT;
        }


        int amplitudeOUTCalculator(int Right_Hand_z)
        {
            int amplitudeOUT = 0;
            if ((Right_Hand_z > rhClose) && (Right_Hand_z < rhFar))
            {

                double A = (double)(Right_Hand_z - rhClose) / (rhFar - rhClose);
                //double B = Max amplitude

                amplitudeOUT = amplitudeMax - (int)(A * amplitudeMax);

                if (amplitudeOUT > amplitudeMax)
                {
                    amplitudeOUT = amplitudeMax;
                }

            }
            if (Right_Hand_z < rhClose)
            {
                amplitudeOUT = amplitudeMax;
            }
            return amplitudeOUT;
        }


        void calculateChannelAmplitudes(Point Right_Hand, int amplitudeOUT)
        {
            //reset amplitudes

            amplitudeWindowSize_Half = amplitudeWindowSize / 2;

            //1
            amplitudeWindowHIGH = amplitudeWindowSize_Half + Right_Hand.Y;
            amplitudeWindowLOW = Right_Hand.Y - amplitudeWindowSize_Half;

            for(int i = 0; i <8; i++){
                channelAmplitudes[i] = 0;
            }

            double upperSegmentSize = 0;
            double lowerSegmentSize = 0;
            int wlocation = -1;//location of the high range of the window within a channel

            //2
            //find where exactly the window is located.
            //will use the high range of the window to scan each channel starting from channel 7-0
            // amplitudewindowhigh > channelhigh[i]
            //if that results in true, that means that channel i+1 is where the window's high range will be located. 

            //Where is the mid point of the window located?
            for (int i = 0; i <= 7; i++)
            {
                if (Right_Hand.Y >= channelBounds[i].getLowerBound() && Right_Hand.Y <= channelBounds[i].getUpperBound())
                {
                    wlocation = i;
                    break;
                }
           } 

            //IF wlocation = -1, then right hand is not within the acceptable range, so do not do anything.
            //so, if wlocation is not -1, then perform splitting and send out amplitudes
            if (!(wlocation == -1))
            {


                //three cases
                //1 - right hand is lower than Mid point of channel 1, which means amplitude is entirely in channel 1
                //2. right hand is higher than mid point of channel 7, which means amplitude is entirely in channel 8
                //3. right hand is in a location where the window spans 2 channels.

                //case 1
                if (Right_Hand.Y <= (amplitudeChannelLow[0] + amplitudeWindowSize_Half))
                {
                    //System.Console.WriteLine("CASE 1");
                    channelAmplitudes[0] = amplitudeOUT;
                }
                //case 2
                else if (Right_Hand.Y >= (amplitudeChannelHigh[7] - amplitudeWindowSize_Half))
                {
                    //System.Console.WriteLine("CASE 2");
                    channelAmplitudes[7] = amplitudeOUT;
                }
                //case 3
                else
                {

                    //System.Console.WriteLine("CASE 3");
                    //use the window location to calculate the area that falls into each channel, 
                    //compute segment sizes, then amplutudes

                    upperSegmentSize = amplitudeChannelHigh[wlocation] - Right_Hand.Y;
                    lowerSegmentSize = Right_Hand.Y - amplitudeChannelLow[wlocation];
                    //System.Console.WriteLine(upperSegmentSize);
                    //System.Console.WriteLine(lowerSegmentSize);


                    if (upperSegmentSize <= lowerSegmentSize)
                    {
                        //this means that signal goes into the higher channel
                        //System.Console.WriteLine("SIGNAL GOES INTO HIGHER CHANNEL");
                        channelAmplitudes[wlocation] = (int)(amplitudeOUT * (upperSegmentSize / (amplitudeWindowSize)));
                        channelAmplitudes[wlocation + 1] = (int)(amplitudeOUT * ((amplitudeWindowSize - upperSegmentSize) / (amplitudeWindowSize)));
                        //System.Console.WriteLine("channelAmplitudes[wlocation] " + channelAmplitudes[wlocation]);
                        //System.Console.WriteLine("channelAmplitudes[wlocation+1] " + channelAmplitudes[wlocation + 1]);
                    }
                    else if (upperSegmentSize > lowerSegmentSize)
                    {
                        //this means that the signal goes into the lower channel
                        channelAmplitudes[wlocation] = (int)(amplitudeOUT * (lowerSegmentSize / (amplitudeWindowSize)));
                        channelAmplitudes[wlocation - 1] = (int)(amplitudeOUT * ((amplitudeWindowSize - lowerSegmentSize) / (amplitudeWindowSize)));
                        //System.Console.WriteLine("SIGNAL GOES INTO LOWER CHANNEL");
                        //System.Console.WriteLine("channelAmplitudes[wlocation] " + channelAmplitudes[wlocation]);
                        //System.Console.WriteLine("channelAmplitudes[wlocation-1] " + channelAmplitudes[wlocation - 1]);
                    }
                }

            }
        }


        void calculateChannelAmplitudes2(Point Right_Hand, int amplitudeOUT)
        {
            /*use the right hand location to calculate the bounds of the three windows. 
             * Then calculate the channel amplitudes one by one. 
             * more details are in written notes. 
             */

            //Calculating MidWindow.. location 1. 
            windowBounds[1].setUpperBound((int) (Right_Hand.Y + (MidWindowSize/2) ));
            windowBounds[1].setLowerBound((int)(Right_Hand.Y - (MidWindowSize/2)));
            windowBounds[1].setStrength(80.0);

            //calculating LowerWindow.. location 0
            windowBounds[0].setUpperBound(windowBounds[1].getLowerBound());
            windowBounds[0].setLowerBound((windowBounds[0].getUpperBound() - NeighborWindowSize));
            windowBounds[0].setStrength(10.0);

            //calculating upper window.. location 2
            windowBounds[2].setLowerBound(windowBounds[1].getUpperBound());
            windowBounds[2].setUpperBound(windowBounds[2].getLowerBound() + NeighborWindowSize);
            windowBounds[2].setStrength(10.0);
            
            //System.Console.Out.WriteLine("Windowbounds[0].Lower = " + windowBounds[0].getLowerBound());
            //System.Console.Out.WriteLine("Windowbounds[0].Upper = " + windowBounds[0].getUpperBound());
            //System.Console.Out.WriteLine("Windowbounds[1].Lower = " + windowBounds[1].getLowerBound());
            //System.Console.Out.WriteLine("Windowbounds[1].Upper = " + windowBounds[1].getUpperBound());
            //System.Console.Out.WriteLine("Windowbounds[2].Lower = " + windowBounds[2].getLowerBound());
            //System.Console.Out.WriteLine("Windowbounds[2].Upper = " + windowBounds[2].getUpperBound());

            //System.Console.Out.WriteLine("MidWindowSize = " + MidWindowSize);
            //System.Console.Out.WriteLine("amplitudeOUT = " + amplitudeOUT);
            //System.Console.Out.WriteLine("amplitudeMax = " + amplitudeMax);

            /* 
             * check each band to see which windows lie in the channels
             * based on the area of the windows in the channel, set the channel strengths
             * normalize the channel strenghts to that the 80% gets normalized to 100%
             */

            for(int i = 0; i <8; i++){
                channelAmplitudes[i] = 0;
            }


            bool[] case12 = new bool[3];
            case12[0] = true; //true means still valid for checkings
            case12[1] = true;
            case12[2] = true;

            for (int i = 0; i < 3; i++)
            {
                //check case one
                //check case two
                //check case three
                //if case seems true, end iteration. 


                //checking case 1. the end case where the entire window falls inside a channel (at the lowest channel)     
                if ((windowBounds[i].getUpperBound() <= channelBounds[0].getUpperBound())
                    && (windowBounds[i].getLowerBound() <= channelBounds[0].getLowerBound()))
                {

                    //System.Console.Out.WriteLine("Case 1 Window[" + i + "] is " + 100 + "% within channel[" + 0 + "]");
                    channelAmplitudes[0] += windowBounds[i].getStrength();
                    windowBounds[i].adjustStrength(windowBounds[i].getStrength());
                    //System.Console.Out.WriteLine("Window[" + i + "] remaining strength is " + windowBounds[i].getStrength());
                    case12[i] = false;
                }

                //checking case 2. the end case where the entire window falls inside a channel (at the highest channel)
                if ((windowBounds[i].getLowerBound() >= channelBounds[7].getLowerBound())
                    && (windowBounds[i].getUpperBound() >= channelBounds[7].getUpperBound()))
                {

                    //System.Console.Out.WriteLine("Case 2 Window[" + i + "] is " + +100 + "% within channel[" + 7 + "]");
                    channelAmplitudes[7] += windowBounds[i].getStrength();
                    windowBounds[i].adjustStrength(windowBounds[i].getStrength());
                    //System.Console.Out.WriteLine("Window[" + i + "] remaining strength is " + windowBounds[i].getStrength());
                    case12[i] = false;
                }

                for (int j = 0; j < 8; j++)
                {
                    if (channelBounds[j].getUpperBound() <= windowBounds[i].getUpperBound()
                        && channelBounds[j].getUpperBound() >= windowBounds[i].getLowerBound()
                        && channelBounds[j].getLowerBound() <= windowBounds[i].getLowerBound()
                        && case12[i])
                    {
                        //calculate the percentage that the window is inside the channel

                        double p;
                        p = 100 * ((channelBounds[j].getUpperBound() - windowBounds[i].getLowerBound())
                            / (windowBounds[i].getUpperBound() - windowBounds[i].getLowerBound()));

                        //System.Console.Out.WriteLine("Case 3.a Window[" + i + "] is " +p + "% within channel[" + j + "]");
                        channelAmplitudes[j] += ((p * windowBounds[i].getStrength()) / 100);

                        if (p == 100)
                        {
                            windowBounds[i].adjustStrength((windowBounds[i].getStrength()));
                        }
                        //System.Console.Out.WriteLine("Window[" + i + "] remaining strength is " + windowBounds[i].getStrength());
                    }

                    if (channelBounds[j].getLowerBound() >= windowBounds[i].getLowerBound()
                        && channelBounds[j].getLowerBound() <= windowBounds[i].getUpperBound()
                        && channelBounds[j].getUpperBound() >= windowBounds[i].getUpperBound()
                        && case12[i])
                    {
                        //calculate the percentage that the window is inside the channel
                        double p;

                        p = 100 * ((windowBounds[i].getUpperBound() - channelBounds[j].getLowerBound())
                            / (windowBounds[i].getUpperBound() - windowBounds[i].getLowerBound()));

                        //System.Console.Out.WriteLine("Case 3.b Window[" + i + "] is " + p +"% within channel[" + j + "]");
                        channelAmplitudes[j] +=((p * windowBounds[i].getStrength()) / 100);
                        if (p == 100)
                        {
                            windowBounds[i].adjustStrength((windowBounds[i].getStrength()));
                        }
                        //System.Console.Out.WriteLine("Window[" + i + "] remaining strength is " + windowBounds[i].getStrength());
                    }
                }
            }

            //Normalize channelamplitudes based on Maxamplitude
            //find the largest amplitude in the array
            //normalize that amplitude value to max amplitude, then multiply the remaining amplitudes to correspond.
            double min = -100;
            for (int a = 0; a <= 7; a++)
            {
                if (channelAmplitudes[a] > min)
                    min = channelAmplitudes[a];
            }

            double normalizeValue = amplitudeOUT / min;
            for (int a = 0; a <= 7; a++)
            {
                channelAmplitudes[a] = channelAmplitudes[a] * normalizeValue;
            }

        }


        void OSCStreamSender(int frequencyOUT, int amplitudeOUT)
        {

            //praparing to send UDP message
            OscBundle bundle = new OscBundle();

            OscMessage message = new OscElement("/joint/element/Frequency", frequencyOUT);
            bundle.AddElement(message);
            message = new OscElement("/joint/element/Amplitude", amplitudeOUT);
            bundle.AddElement(message);

            for (int i = 0; i <= 7; i++)
            {
                message = new OscElement("/joint/element/ChannelAmplitude/" + i, (int)channelAmplitudes[i]);
                bundle.AddElement(message);
            }

            OSCSender.Send(bundle);

        }

        void OSCHandler(Point Left_Hand, Point Right_Hand, int Right_Hand_z, SkeletonData data)
        {
            int frequencyOUT = frequencyOUTCalculator(Left_Hand, Right_Hand);
            int amplitudeOUT = amplitudeOUTCalculator(Right_Hand_z);

            //calculateChannelAmplitudes(Right_Hand, amplitudeOUT);
            calculateChannelAmplitudes2(Right_Hand, amplitudeOUT);

            //only send message if unmuted
            OSCStreamSender(frequencyOUT, amplitudeOUT);
        }


        void OSCHandler2(Point Left_Hand, Point Right_Hand, int Right_Hand_z, SkeletonData data)
        {
            int frequencyOUT = frequencyOUTCalculator(Left_Hand, Right_Hand);
            int amplitudeOUT = amplitudeOUTCalculator(Right_Hand_z);
            
            //calculateChannelAmplitudes(Right_Hand, amplitudeOUT);
            calculateChannelAmplitudes2(Right_Hand, amplitudeOUT);


            //only send message if unmuted
            OSCStreamSender(frequencyOUT, amplitudeOUT);
        }
        
        
        
        Point returnPoint(double a, double b)
        {
            Point point = new Point(a,b);
            return point;
        }

        void drawLineOnCanvas(Canvas skeleton, PointCollection points, int strokeThickness, Brush brush)
        {
            Polyline Line = new Polyline();
            Line.Points = points;
            Line.Stroke = brush;
            Line.StrokeThickness = strokeThickness;
            skeleton.Children.Add(Line);
        }


        void drawCountDownTimer(Canvas skeleton)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = ""+ countdown + "...";
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 0));
            textBlock.FontSize = 40;
            Canvas.SetLeft(textBlock, skeleton.Width - 75);
            Canvas.SetTop(textBlock, skeleton.Height - (skeleton.Height - 50));
            skeleton.Children.Add(textBlock);

            //System.Console.Out.WriteLine("yeah, writing text now");
        }


        void drawMeter(Canvas skeleton)
        {

            /*get the three zones info from the freq hand 
             * get the max range for the right hand. spacial encoding information. 
             * and maybe also have a circle which changes colour based on how much energy is inputted into the signal.(signal strength)
             */
            
            /*i can use the calibrationdata structure:
             * Meter1 goes from lefthand_elbowslow to left hand no elbows low
             * Meter2 goes from lefthand_noelbows_low to lefthand_no elbows_high
             * Meter3 goes from lefthand_noelbows_high to lefthand_elbows_high 
             */
            double freqMeter1 = skeleton.Height - calibrationData.LEFTHAND_ELBOWS_LOW.Y;
            double freqMeter2 = skeleton.Height - calibrationData.LEFTHAND_NOELBOWS_LOW.Y;
            double freqMeter3 = skeleton.Height - calibrationData.LEFTHAND_NOELBOWS_HIGH.Y;
            double freqMeter4 = skeleton.Height - calibrationData.LEFTHAND_ELBOWS_HIGH.Y;
            double freqX = 100;

            double ampMeter1 = skeleton.Height - calibrationData.RIGHTHAND_ELBOWS_HIGH.Y;
            double ampMeter2 = skeleton.Height - calibrationData.RIGHTHAND_ELBOWS_LOW.Y;
            double ampX = 400;

            Polyline Line = new Polyline();
            Polyline LineAmp = new Polyline();
            Polyline LinefreqDivider = new Polyline();
            PointCollection points = new PointCollection();
            PointCollection pointsAmp = new PointCollection();
            Brush brush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            int strokeThickness = 3;
            /*The segment which creates a C shape bracket will be created using the point list*/

            points.Add(new Point(freqX + 10,freqMeter4));
            points.Add(new Point(freqX, freqMeter4));
            points.Add(new Point(freqX, freqMeter1));
            points.Add(new Point(freqX+10, freqMeter1));


            //Line.Points = points;
            //Line.Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            //Line.StrokeThickness = 3;
            //skeleton.Children.Add(Line);
            drawLineOnCanvas(skeleton, points, strokeThickness, brush);


            PointCollection pointsfreqDivider = new PointCollection();
            pointsfreqDivider.Add(new Point(freqX + 10, freqMeter3));
            pointsfreqDivider.Add(new Point(freqX, freqMeter3));
            pointsfreqDivider.Add(new Point(freqX, freqMeter2));
            pointsfreqDivider.Add(new Point(freqX + 10, freqMeter2));
            brush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            drawLineOnCanvas(skeleton, pointsfreqDivider, strokeThickness, brush);

            pointsAmp.Add(new Point(ampX-10, ampMeter1));
            pointsAmp.Add(new Point(ampX, ampMeter1));
            pointsAmp.Add(new Point(ampX, ampMeter2));
            pointsAmp.Add(new Point(ampX-10, ampMeter2));
            brush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
            drawLineOnCanvas(skeleton, pointsAmp, strokeThickness, brush);
            //LineAmp.Points = pointsAmp;
            //LineAmp.Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            //LineAmp.StrokeThickness = 3;
            //skeleton.Children.Add(LineAmp);

            


            /* similarly i can use the calibration structure to do the following:
             * get the right hand absolute high, and right hand absolute low. 
             * and that becomes the meter which helps the users
             */

            /*colours for the meter?
             * draw segments, and their width?
             */
        }

        void drawcircles(SkeletonData data)
        {
            //get the location of the right hand
            //get the location of the left hand

            //draw a circle..
            //then resize based on various parameters, talk to mike about this.
            //Resizing parameters: 
            /*
             * Right Ellipse
             * Make it bigger and smaller based on the amplitude of the right hand.
             * Make it change colour as it moves up and down? Gradients might be tough.
             * 
             * Left Ellipse
             * Make the brush stroke get thinner and thicker as the left hand moves high and low.
             * 
             * Make them both go red if they are out of range?
             * 
             */

            Point rh = getJointLocation(data.Joints, JointID.HandRight);
            Point lh = getJointLocation(data.Joints, JointID.HandLeft);

            double rhz = zScale *(data.Joints[JointID.HandRight].Position.Z);
            int amplitudeOut = amplitudeOUTCalculator((int)rhz);
            double rhzAmplitude = (double)amplitudeOut / (double)amplitudeMax;


            double ellipseMaxSize = 75;
            double ellipseMaxThickness = 15;
            double rightEllipseSize = ellipseMaxSize * rhzAmplitude;
            double rightEllipseThickness = ellipseMaxThickness * rhzAmplitude;

            if (rightEllipseSize < 0)
            {
                rightEllipseSize = 10;
            }
            if (rightEllipseThickness < 0)
            {
                rightEllipseThickness = 2;
            }


            Ellipse rhEllipse = new Ellipse();
            // Describes the brush's color using RGB values. 
            // Each value has a range of 0-255.
            rhEllipse.StrokeThickness = rightEllipseThickness;
            rhEllipse.Stroke = Brushes.White;

            // Set the width and height of the Ellipse.
            rhEllipse.Width = rightEllipseSize;
            rhEllipse.Height = rightEllipseSize;
            Canvas.SetLeft(rhEllipse, rh.X - (rhEllipse.Width/2));
            Canvas.SetTop(rhEllipse, rh.Y - (rhEllipse.Width / 2));
            GestureCanvas.Children.Add(rhEllipse);

            double lhzAmplitude = (double)(frequencyMax - freqOut) / (double)frequencyMax;
            double leftEllipseSize = ellipseMaxSize * lhzAmplitude;
            double leftEllipseThickness = ellipseMaxThickness * lhzAmplitude;
            
            if (leftEllipseThickness < 0)
            {
                leftEllipseThickness = 2;
            }

            if ((int)freqOut == 0)
            {
                leftEllipseThickness = 0;
            }

            Ellipse lhEllipse = new Ellipse();
            // Describes the brush's color using RGB values. 
            // Each value has a range of 0-255.
            lhEllipse.StrokeThickness = leftEllipseThickness;
            lhEllipse.Stroke = Brushes.GreenYellow;

            // Set the width and height of the Ellipse.
            lhEllipse.Width = 40;
            lhEllipse.Height = 40;
            Canvas.SetLeft(lhEllipse, lh.X - 20);
            Canvas.SetTop(lhEllipse, lh.Y - 20);
            GestureCanvas.Children.Add(lhEllipse);

        }

        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            int iSkeleton = 0;
            Brush[] brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            skeleton.Children.Clear();
            GestureCanvas.Children.Clear();
            //System.Console.Out.WriteLine("see image");
            Canvas.SetTop(video, 0);
            Canvas.SetLeft(video, 0);
            skeleton.Children.Add(video);

            if (countdownTimer.IsEnabled)
                drawCountDownTimer(skeleton);

            drawMeter(skeleton);
            drawMeter(GestureCanvas);


            //System.Console.WriteLine("Mute Status is: " + mute);
            int skelCount = 0;
            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {

                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    skelCount++;
                    // Draw bones
                    Brush brush = brushes[iSkeleton % brushes.Length];
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft));
                    skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight));

                    //This is where the program will communicate with max msp and send commands regarding which channels should be used.
                    //There I will calculate the x,y screen positions of the left and right hands. compare them to find out which zone they are in. 
                    //then assign a frequency, and amplitude to the output signal. 
                    //send the output through UDP to max/msp 
                    //allteststring = allteststring + "Left hand: " + Left_Hand.ToString() + "\n";

                    //program still tracks, but does not play anything while the mute is activated
                    if (mute == false)
                    {

                        //The program will draw the circles here
                        drawcircles(data);

                        //find out which zone the left hand is in, and figure out the frequency that's to be played
                        Point Left_Hand;
                        Left_Hand = getJointLocation(data.Joints, JointID.HandLeft);
                        Point Right_Hand;
                        Right_Hand = getJointLocation(data.Joints, JointID.HandRight);
                        //adjust the Y coordinates on the left and right hand. 
                        Left_Hand.Y = screenY - (int)Left_Hand.Y;
                        Right_Hand.Y = screenY - (int)Right_Hand.Y;
                        int Right_Hand_z = (int)(zScale * data.Joints[JointID.HandRight].Position.Z);

                        //OSCHandler(Left_Hand, Right_Hand, Right_Hand_z, data);
                        OSCHandler2(Left_Hand, Right_Hand, Right_Hand_z, data);

                        string rightlog = Right_Hand.X.ToString() + "," + Right_Hand.Y.ToString() + "," + Right_Hand_z;
                        string leftlog = Left_Hand.X.ToString() + "," + Left_Hand.Y.ToString();
                        string freqlog = freqOut.ToString();
                        string strengthlog = amplitudeOUTCalculator(Right_Hand_z).ToString();


                        if (record)
                        {
                            recordLog.WriteLine(rightlog + "/" + leftlog + "/" + freqlog + "/" + strengthlog + "/" + timestamp); //include the time elapsed count in this
                            //System.Console.WriteLine("Recording...");
                        }
                    }
                }
                iSkeleton++;
            } // for each skeleton

            if (skelCount == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    channelAmplitudes[i] = 0;
                }
                freqOut2 = 0;
                OSCStreamSender(0, 0);
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //File.WriteAllText(testfilename, allteststring);
            recordLog.Close();
        }

        private void ReplayStimulusButton_Click(object sender, RoutedEventArgs e)
        {

            if (recordTimer.IsEnabled == true)
                recordTimer.Stop();

            //NextStimulusButton.Content = "Next >>";
            //NextStimulusButton.FontWeight = FontWeights.Normal;
            //NextStimulusButton.Foreground = Brushes.Black;
            //increment current stimulus, play next stimulus by sending the instruction to the max patch. (timer works the same way as replay with the mute and unmute)

            vtc = VariableTestCase.returnVariableTestCase((currentStimulus - 1), rangeAmplitude, (int)channelBounds[0].getLowerBound(),0);
            //vtc = VariableTestCase.returnVariableTestCase((20), rangeAmplitude, (int)amplitudeChannelLow[0]);



            mute = true;
            //sending a zero command
            Point p = new Point(0, 0);
            calculateChannelAmplitudes2(p, 0);
            OSCStreamSender(0, 0);

            //Point rightHand = new Point(0, testParams[1]); // we do not need the x, coordinate on the right hand. 
            //calculateChannelAmplitudes(rightHand, testParams[2]);

            //OSCStreamSender(testParams[0], testParams[2]); //(freqOUT, and amplitudeOUT)
            //send the parameters to the method which makes max/msp play the sound out

            Muter = new DispatcherTimer();
            Muter.Interval = new TimeSpan(0, 0, 0,vtc.timespan); //this is timespan + time delay
            Muter.Tick += new EventHandler(Muter_Tick);
            Muter.Start();

            vtcTimer = new DispatcherTimer();
            vtcTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            vtcTimer.Tick += new EventHandler(vtcTimer_Tick);
            vtcTimer.Start();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)(vtc.timedelay) / 3);
            countdownTimer.Tick += new EventHandler(countdownTimer_Tick);
            countdown = 3;
            countdownTimer.Start();

            //NextStimulusButton.IsEnabled = false;
            //ReplayStimulusButton.IsEnabled = false;
            //BackButton.IsEnabled = false;
            //FinalAttemptButton.IsEnabled = false;

            //ReplayStimulusButton.IsEnabled = true;
            replaycount++;
            recordTag = "\n\n/**** The Replay Button has been pressed " + replaycount + "****/\n\n";
            recordLog.WriteLine(recordTag);
        }

        private void vtcTimer_Tick(object sender, EventArgs e)
        {
            //add 100ms to the counter
            //check if the counter is bigger than the delay
            //check if the next tick takes 100 ms, but the remaining delay is only less tahn 100ms. not really necessary
            //if so, stop the clock.

            vtcCounter += 100;
            int offset = 500;//500 ms offset

            if (vtcCounter > vtc.timedelay)
            {
                //this is when the program does the frequency and strength calculations and calls osc sender.

                /*get the required variables
                 * new frequency, 
                 * new channel amplitudes
                 * new strength
                 * */

                double frequency = vtc.initialFrequency + (vtc.rateOfChangeFrequency * (vtcCounter-offset)/100);
                //System.Console.WriteLine("frequency is : " + frequency);
                //System.Console.WriteLine("calc is : " + ((double)vtcCounter / (double)vtc.timespan));
                if (vtc.initialFrequency > vtc.finalFrequency)
                {
                    if (frequency <= vtc.finalFrequency)
                    {
                        frequency = vtc.finalFrequency;
                    }   
                }
                if (vtc.initialFrequency < vtc.finalFrequency)
                {
                    if (frequency >= vtc.finalFrequency)
                    {
                        frequency = vtc.finalFrequency;
                    }   
                }

                double strength = vtc.initialSignalStrength + (vtc.rateOfChangeSignalStrength * (vtcCounter - offset) / 100);
                if (vtc.initialSignalStrength > vtc.finalSignalStrength)
                {
                    if (strength < vtc.finalSignalStrength)
                    {
                        strength = vtc.finalSignalStrength;
                    }
                }
                if (vtc.initialSignalStrength < vtc.finalSignalStrength)
                {
                    if (strength > vtc.finalSignalStrength)
                    {
                        strength = vtc.finalSignalStrength;
                    }
                }


                double scaledAmplitude = (amplitudeMax * strength) / 100;

                double rhc = vtc.initialRHC + (vtc.rateOfChangeRHC * (vtcCounter-offset)/100);
                if (vtc.initialRHC > vtc.finalRHC)
                {
                    if (rhc < vtc.finalRHC)
                    {
                        rhc = vtc.finalRHC;
                    }
                }
                if (vtc.initialRHC < vtc.finalRHC)
                {
                    if (rhc > vtc.finalRHC)
                    {
                        rhc = vtc.finalRHC;
                    }
                }

                //System.Console.Out.WriteLine("Frequency: " + frequency);
                //System.Console.Out.WriteLine("vtc.initialFrequency: " + vtc.initialFrequency);
                //System.Console.Out.WriteLine("(vtc.rateOfChangeFreq * vtcCounter / 100): " + (vtc.rateOfChangeFrequency * (vtcCounter-offset)/100));
                //System.Console.Out.WriteLine("vtcCounter: " + vtcCounter);
                //System.Console.Out.WriteLine("rhc: " + rhc);
                //System.Console.Out.WriteLine("scaledAmplitude: " + scaledAmplitude);

                Point r = new Point(0,rhc);
                calculateChannelAmplitudes2(r, (int)scaledAmplitude);

                //osc send
                OSCStreamSender((int)frequency, (int)scaledAmplitude);

                //System.Console.WriteLine("freq = " + (int)frequency + "\nstrength = " + (int)scaledAmplitude);
                //System.Console.WriteLine("vtcCounter is at " + vtcCounter);
                //System.Console.WriteLine("comparing to " + ((vtc.timedelay * 2) + vtc.timespan));
                if (vtcCounter > ((vtc.timedelay * 2) + vtc.timespan))
                {
                    //osc send
                    Point p = new Point(0, 0);

                    record = true;
                    vtcTimer.Stop();
                    //if (Muter.IsEnabled)
                    Muter.Stop();
                    recordTimer.Start();

                    vtcCounter = 0;
                    mute = false;

                    calculateChannelAmplitudes2(p, 0);
                    OSCStreamSender(0, 0);

                    ////System.Console.WriteLine("Muter, and vtcTimer are off");
                    //if (finalAttempt == false)
                    //{
                    //    NextStimulusButton.IsEnabled = true;
                    //    ReplayStimulusButton.IsEnabled = true;
                    //    BackButton.IsEnabled = true;
                    //    FinalAttemptButton.IsEnabled = true;
                    //}
                    //if (finalAttempt == true)
                    //    NextStimulusButton.IsEnabled = true;
                }
            }
        }

        private void NextStimulusButton_Click(object sender, RoutedEventArgs e)
        {
            finalAttempt = false;
            /*Change the recordLog streamwriter variable so that a new file is created each time. 
             */
            recordLog.Close();
            string newfilename = recordFilename + "_" + currentStimulus + ".txt";
            recordLog = new StreamWriter(newfilename);
            replaycount = 0;
            if (recordTimer.IsEnabled == true)
                recordTimer.Stop();

            //NextStimulusButton.Content = "Next >>";
            //NextStimulusButton.FontWeight = FontWeights.Normal;
            //NextStimulusButton.Foreground = Brushes.Black;
            //increment current stimulus, play next stimulus by sending the instruction to the max patch. (timer works the same way as replay with the mute and unmute)

            vtc = VariableTestCase.returnVariableTestCase((currentStimulus), rangeAmplitude, (int)channelBounds[0].getLowerBound(),1);
            string newvtcFilename = recordFilename + "_testcases_" + currentStimulus + ".txt";
            vtcLog = new StreamWriter(newvtcFilename);
            //write everything within the vtc structure
            //vtcLog.WriteLine("timespan = " + vtc.timespan + ";");
            //vtcLog.WriteLine("timedelay = " + vtc.timedelay + ";");
            //vtcLog.WriteLine("initFreq = " + vtc.initialFrequency + ";");
            //vtcLog.WriteLine("finFreq = " + vtc.finalFrequency + ";");
            //vtcLog.WriteLine("initialSignalStrength = " + vtc.initialSignalStrength + ";");
            //vtcLog.WriteLine("finalSignalStrength = " + vtc.finalSignalStrength + ";");
            //vtcLog.WriteLine("initialRHC = " + vtc.initialRHC + ";");
            //vtcLog.WriteLine("finalRHC = " + vtc.finalRHC + ";");
            //vtcLog.WriteLine("rateOfChangeFrequency = " + vtc.rateOfChangeFrequency + ";");
            //vtcLog.WriteLine("rateOfChangeRHC = " + vtc.rateOfChangeRHC + ";");
            //vtcLog.WriteLine("rateOfChangeSignalStrength = " + vtc.rateOfChangeSignalStrength + ";");
            vtcLog.WriteLine(vtc.timespan);
            vtcLog.WriteLine(vtc.timedelay);
            vtcLog.WriteLine(vtc.initialFrequency);
            vtcLog.WriteLine(vtc.finalFrequency);
            vtcLog.WriteLine(vtc.initialSignalStrength);
            vtcLog.WriteLine(vtc.finalSignalStrength);
            vtcLog.WriteLine(vtc.initialRHC);
            vtcLog.WriteLine(vtc.finalRHC);
            vtcLog.WriteLine(vtc.rateOfChangeFrequency);
            vtcLog.WriteLine(vtc.rateOfChangeRHC);
            vtcLog.WriteLine(vtc.rateOfChangeSignalStrength);
            vtcLog.Close();


            mute = true;
            
            //sending a zero command
            Point p = new Point(0, 0);
            calculateChannelAmplitudes2(p, 0);
            OSCStreamSender(0, 0);



            //Point rightHand = new Point(0, testParams[1]); // we do not need the x, coordinate on the right hand. 
            //calculateChannelAmplitudes(rightHand, testParams[2]);

            //OSCStreamSender(testParams[0], testParams[2]); //(freqOUT, and amplitudeOUT)
            //send the parameters to the method which makes max/msp play the sound out

            Muter = new DispatcherTimer();
            Muter.Interval = new TimeSpan(0, 0, 0, vtc.timespan); //this is timespan + time delay
            Muter.Tick += new EventHandler(Muter_Tick);
            Muter.Start();

            vtcTimer = new DispatcherTimer();
            vtcTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            vtcTimer.Tick += new EventHandler(vtcTimer_Tick);
            vtcTimer.Start();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)(vtc.timedelay) / 3);
            countdownTimer.Tick += new EventHandler(countdownTimer_Tick);
            countdown = 3;
            countdownTimer.Start();

            //NextStimulusButton.IsEnabled = false;
            //ReplayStimulusButton.IsEnabled = false;
            //BackButton.IsEnabled = false;
            //FinalAttemptButton.IsEnabled = false;

            ////ReplayStimulusButton.IsEnabled = true;
            ////recordTag = "\nTag: currentStimulus is at " + currentStimulus + "\n";
            ////recordLog.WriteLine(recordTag);
            ////System.Console.WriteLine(recordTag);
            //currentStimulus++;
            
            ////System.Console.WriteLine("current stimulus: " + currentStimulus);
            ////now play next stimulus
            //testcaseProgress.Content = "" + currentStimulus + " /" + totalstimuli;
        }

        private void Muter_Tick(object sender, EventArgs e)
        {
            if (mute)
            {
                mute = false;
                //if mute is false, it means that program is running, this means that we need to record with the timer reset from start
                timestamp = 0;
                record = true;
                Muter.Stop();
                //System.Console.WriteLine("muterstop");
                recordTimer.Start();
            }
            else 
            {
                mute = true;
                //System.Console.WriteLine("Mute Tick changed mute to: " + mute.ToString());
                //System.Console.WriteLine("muterstop");
                Muter.Stop();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //set an RHC
            //calculate everything based on it

            int rhc = 260;
            Point rh =  new Point (0, rhc);
            calculateChannelAmplitudes2(rh, 150);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            finalAttempt = false;
            currentStimulus--;
            /*Change the recordLog streamwriter variable so that a new file is created each time. 
             */
            replaycount = 0;


            recordLog.Close();
            string newfilename = recordFilename + "_" + currentStimulus + ".txt";
            recordLog = new StreamWriter(newfilename);

            //ReplayStimulusButton.IsEnabled = true;
            //if (recordTimer.IsEnabled == true)
            //    recordTimer.Stop();

            //NextStimulusButton.Content = "Next >>";
            //NextStimulusButton.FontWeight = FontWeights.Normal;
            //NextStimulusButton.Foreground = Brushes.Black;
            //increment current stimulus, play next stimulus by sending the instruction to the max patch. (timer works the same way as replay with the mute and unmute)

            vtc = VariableTestCase.returnVariableTestCase((currentStimulus), rangeAmplitude, (int)channelBounds[0].getLowerBound(),1);
            //vtc = VariableTestCase.returnVariableTestCase((20), rangeAmplitude, (int)amplitudeChannelLow[0]);

            mute = true;
            //sending a zero command
            Point p = new Point(0, 0);
            calculateChannelAmplitudes2(p, 0);
            OSCStreamSender(0, 0);

            //Point rightHand = new Point(0, testParams[1]); // we do not need the x, coordinate on the right hand. 
            //calculateChannelAmplitudes(rightHand, testParams[2]);

            //OSCStreamSender(testParams[0], testParams[2]); //(freqOUT, and amplitudeOUT)
            //send the parameters to the method which makes max/msp play the sound out

            Muter = new DispatcherTimer();
            Muter.Interval = new TimeSpan(0, 0, 0, vtc.timespan); //this is timespan + time delay
            Muter.Tick += new EventHandler(Muter_Tick);
            Muter.Start();

            vtcTimer = new DispatcherTimer();
            vtcTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            vtcTimer.Tick += new EventHandler(vtcTimer_Tick);
            vtcTimer.Start();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)(vtc.timedelay) / 3);
            countdownTimer.Tick += new EventHandler(countdownTimer_Tick);
            countdown = 3;
            countdownTimer.Start();

            //NextStimulusButton.IsEnabled = false;
            //ReplayStimulusButton.IsEnabled = false;
            //BackButton.IsEnabled = false;
            //FinalAttemptButton.IsEnabled = false;

            ////ReplayStimulusButton.IsEnabled = true;
            ////recordTag = "\nTag: currentStimulus is at " + currentStimulus + "\n";
            ////recordLog.WriteLine(recordTag);
            ////System.Console.WriteLine(recordTag);


            //testcaseProgress.Content = "" + currentStimulus + " /" + totalstimuli;

            //System.Console.WriteLine("current stimulus: " + currentStimulus);
            //now play next stimulus

        }

        private void FinalAttemptButton_Click(object sender, RoutedEventArgs e)
        {
            finalAttempt = true;
            if (recordTimer.IsEnabled == true)
            //    recordTimer.Stop();

            //NextStimulusButton.Content = "Next >>";
            //NextStimulusButton.FontWeight = FontWeights.Normal;
            //NextStimulusButton.Foreground = Brushes.Black;
            ////increment current stimulus, play next stimulus by sending the instruction to the max patch. (timer works the same way as replay with the mute and unmute)

            vtc = VariableTestCase.returnVariableTestCase((currentStimulus - 1), rangeAmplitude, (int)channelBounds[0].getLowerBound(), 0);
            //vtc = VariableTestCase.returnVariableTestCase((20), rangeAmplitude, (int)amplitudeChannelLow[0]);



            mute = true;
            //sending a zero command
            Point p = new Point(0, 0);
            calculateChannelAmplitudes2(p, 0);
            OSCStreamSender(0, 0);

            //Point rightHand = new Point(0, testParams[1]); // we do not need the x, coordinate on the right hand. 
            //calculateChannelAmplitudes(rightHand, testParams[2]);

            //OSCStreamSender(testParams[0], testParams[2]); //(freqOUT, and amplitudeOUT)
            //send the parameters to the method which makes max/msp play the sound out

            Muter = new DispatcherTimer();
            Muter.Interval = new TimeSpan(0, 0, 0, vtc.timespan); //this is timespan + time delay
            Muter.Tick += new EventHandler(Muter_Tick);
            Muter.Start();

            vtcTimer = new DispatcherTimer();
            vtcTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            vtcTimer.Tick += new EventHandler(vtcTimer_Tick);
            vtcTimer.Start();

            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)(vtc.timedelay) / 3);
            countdownTimer.Tick += new EventHandler(countdownTimer_Tick);
            countdown = 3;
            //countdownTimer.Start();

            //NextStimulusButton.IsEnabled = false;
            //ReplayStimulusButton.IsEnabled = false;
            //BackButton.IsEnabled = false;
            //FinalAttemptButton.IsEnabled = false;

            //ReplayStimulusButton.IsEnabled = true;
            recordTag = "\n\n**** THIS IS THE FINAL ATTEMPT ****\n\n";
            recordLog.WriteLine(recordTag);
        }

    }
}
