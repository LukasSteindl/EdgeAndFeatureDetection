using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Edgedetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            input.Source = BitmapToImageSource(Properties.Resources.home);
            button_Click(null, null);
        }
        Bitmap grayscaleinput;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ImageSource img = input.Source;
            
            Bitmap b = Properties.Resources.home;
            Console.WriteLine(b.GetPixel(0, 1).R);
            Console.WriteLine(b.GetPixel(0, 1).G);
            Console.WriteLine(b.GetPixel(0, 1).B);

            Bitmap grayscale = new Bitmap(b.Width, b.Height);
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                {
                    System.Drawing.Color c = b.GetPixel(x, y);
                    int gray = Convert.ToInt32(0.3 * Convert.ToDouble(c.R) + 0.59 * Convert.ToDouble(c.G) + 0.11 * Convert.ToDouble(c.B));
                    grayscale.SetPixel(x, y, System.Drawing.Color.FromArgb(255, gray, gray, gray));
                }
            grayscaleinput = grayscale; //setze den grayscale input für weitere edgedetection
            output.Source = BitmapToImageSource(grayscale);


        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //sobel filterkern
            int[,] sobelkernel = new int[,]
            { { 0, 0, 0, 0 ,0},
               {0,-1,-2,-1, 0},
               {0, 0, 0, 0, 0},
               {0, 1, 2, 1, 0},
               {0, 0, 0, 0 ,0}};


            int[,] sobelkernel2 = new int[,]
             { {0, 0, 0, 0 ,0},
               {0,-1, 0, 1, 0},
               {0, -2, 0, 2, 0},
               {0, -1, 0, 1, 0},
               {0, 0, 0, 0 ,0}};

            Bitmap output = new Bitmap(grayscaleinput.Width, grayscaleinput.Height);

            for (int x = 2; x < grayscaleinput.Width - 2; x++)  //+2 and -2 to avoid the corner problem
                for (int y = 2; y < grayscaleinput.Height - 2; y++)
                {
                    //copy existing pixels to empty 5x5 grid to avoid corner issues
                    int[,] subimage = getSubimage(x, y, 5, 2);
                    double activationstrenght = Math.Sqrt(Math.Pow(convolution(sobelkernel, subimage), 2) + Math.Pow(convolution(sobelkernel2, subimage), 2));
                    int n = 255-Convert.ToInt32(activationstrenght * 0.31);
                    output.SetPixel(x, y, System.Drawing.Color.FromArgb(n, n, n));
                }
            outputedges.Source = BitmapToImageSource(output);
        }


        private int[,] getSubimage(int x, int y, int size, int centeroffset)
        //size should be an odd number > 1, centeroffset indicates the number of pixels left from the provided point 
        //eg. if subimage square size is 5, 
        //then the provided point is in array position 2, and there are 2 pixels left 
        //from this point position 0 and position 1 in the image patch
        {
            int[,] subimage = new int[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    subimage[i, j] = grayscaleinput.GetPixel(x + i - centeroffset, y + j - centeroffset).G;
                }
            return subimage;
        }

        private double convolution(int[,] kernel, int[,] image)
        {
            double sum = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                    sum += image[i, j] * kernel[i, j];
            }
            return sum;

        }

    


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (grayscaleinput != null)
                detect_points_of_interest(Convert.ToInt32(Sliderthreshold.Value));
        }

        private void detect_points_of_interest(int threshold)
        {
            //detecting Points of interest
            List<int[]> directions = new List<int[]>();
            directions.Add(new int[2] { 1, 0 });
            directions.Add(new int[2] { 1, 1 });
            directions.Add(new int[2] { 0, 1 });
            directions.Add(new int[2] { -1, 1 });
            Bitmap output = new Bitmap(grayscaleinput.Width, grayscaleinput.Height);

            int centeroffset = 2;
            for (int x = centeroffset; x < grayscaleinput.Width - centeroffset; x++)  //for every point in the image
                for (int y = centeroffset; y < grayscaleinput.Height - centeroffset; y++)
                {
                    //copy existing pixels to empty 5x5 grid to avoid corner issues
                    int[,] subimage = getSubimage(x, y, 5, centeroffset); //get Wxy an image area around the point 

                    List<int> differences = new List<int>();
                    //für jede Richtung...
                    foreach (int[] d in directions)
                    {
                        //Berechne E als die summe der quadrate aller differenzen eines jeden punktes u, v aus Wxy mit seiner verschiebung in Richtung d
                        int E = 0;
                        for (int u = 1; u < 4; u++) //für jeden Punkt in Wxy (5x5) 
                            for (int v = 1; v < 4; v++) 
                            {
                                E += (subimage[u + d[0], v + d[1]] - subimage[u, v]) * (subimage[u + d[0], v + d[1]] - subimage[u, v]);
                            }
                        differences.Add(E);
                    }
                    //wenn die kleinste differenz hoch ist dann zeichne den pixel im zielbild weiß ein...

                    if (differences.Min() > threshold)
                        output.SetPixel(x, y, System.Drawing.Color.White);
                    else
                        output.SetPixel(x, y, System.Drawing.Color.FromArgb(255, grayscaleinput.GetPixel(x, y).G, grayscaleinput.GetPixel(x, y).G, grayscaleinput.GetPixel(x, y).G));

                    // Console.WriteLine(differences.Count()); //should be 9
                    //Console.WriteLine(subimage.Length); //Should be 9
                }
            Merkmal.Source = BitmapToImageSource(output);

        }
        

        private void Sliderthreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
         
        }
    }
}
