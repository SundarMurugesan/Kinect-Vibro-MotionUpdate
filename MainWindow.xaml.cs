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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFPrototype
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //I might need global variables to keep the skeleton tracked

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InstructionTextBox.Text = "Please follow the following steps in order to run the program: \r\nStep1: Start up Max-Patch that runs with this program\r\nStep2: Start the calibrate by finding the button found in Tools";
        }

        private void Window_Closed(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Begin_Click(object sender, RoutedEventArgs e)
        {
           // CalibrateWindow calibrateWindowForm = new CalibrateWindow();
           // calibrateWindowForm.Show();
            //Gesture Window and initialize. 
            GestureWindow gestureForm = new GestureWindow();
            gestureForm.Show();
            
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            //Loads a saved calibration file for a user
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //Saves the current calibration file
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
