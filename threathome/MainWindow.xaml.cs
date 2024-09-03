using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;

namespace threathome
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ManualResetEvent _pauseEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _copyTask;
        private bool _isCopying = false;
        public MainWindow()
        {
            InitializeComponent();

            _pauseEvent = new ManualResetEvent(true); 
            _cancellationTokenSource = new CancellationTokenSource();
        }


        private void OpenFile_From(object sender, RoutedEventArgs e)
        {
            Thread openfrom = new Thread(OpenFile_from);
            openfrom.SetApartmentState(ApartmentState.STA); 
            openfrom.Start();
        }

        private void OpenFile_To(object sender, RoutedEventArgs e)
        {
            Thread opento = new Thread(openfile_To);
            opento.SetApartmentState(ApartmentState.STA);
            opento.Start();
        }


        private void OpenFile_from()
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Filter = "All files (*.*)|*.*";

            if (openfile.ShowDialog() == true)
            {
                Dispatcher.Invoke(() => openfile_from_txtbox.Text = openfile.FileName);
            }
        }

        private void openfile_To()
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Filter = "All files (*.*)|*.*";

            if (openfile.ShowDialog() == true)
            {
                Dispatcher.Invoke(() => openfile_to_txtbox.Text = openfile.FileName);
            }
        }



        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCopying)
            {
                MessageBox.Show("A copy operation is already in progress.");
                return;
            }

            string sourceFilePath = openfile_from_txtbox.Text;
            string destinationFilePath = openfile_to_txtbox.Text;

            if (File.Exists(sourceFilePath))
            {
                _isCopying = true;
                _cancellationTokenSource = new CancellationTokenSource();
                _pauseEvent.Set(); // İş parçacığını devam ettirmek için aç

                _copyTask = Task.Run(() => CopyFile(sourceFilePath, destinationFilePath, _cancellationTokenSource.Token));
            }
            else
            {
                MessageBox.Show("Source file does not exist.");
            }
        }

        private void CopyFile(string sourceFilePath, string destinationFilePath, CancellationToken token)
        {
            try
            {
                const int bufferSize = 1024;
                long fileSize = new FileInfo(sourceFilePath).Length;
                long bytesCopied = 0;
                double previousPercentage = 0;

                using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;

                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        
                        _pauseEvent.WaitOne();

                        
                        if (token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() => MessageBox.Show("Copy operation aborted!"));
                            break;
                        }

                        destinationStream.Write(buffer, 0, bytesRead);
                        bytesCopied += bytesRead;

                        double progressPercentage = (double)bytesCopied / fileSize * 100;


                        if (progressPercentage >= previousPercentage + 1)
                        {
                            previousPercentage = Math.Floor(progressPercentage);

                            Dispatcher.Invoke(() => progressBar.Value = progressPercentage);
                            Thread.Sleep(1000);
                        }

                       
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("File copied successfully!"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Error copying file: " + ex.Message));
            }
            finally
            {
                _isCopying = false;
                Dispatcher.Invoke(() => progressBar.Value = 0);
            }
        }

        private void suspend_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_isCopying)
            {
                if (_pauseEvent.WaitOne(0)) 
                {
                    _pauseEvent.Reset(); 
                    suspend_btn.Content = "Resume";
                }
                else
                {
                    _pauseEvent.Set(); 
                    suspend_btn.Content = "Suspend";
                }
            }
        }

        private void abort_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_isCopying)
            {
                _cancellationTokenSource.Cancel(); 
                _pauseEvent.Set();
                suspend_btn.Content = "Suspend";
            }
        }
    }
    



}
