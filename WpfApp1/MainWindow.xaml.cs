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

namespace Puzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isRectDragInProg = false;
        bool isFullscreenON = false;
        bool objectsOverlap;
        bool validPlacement = true;
        double lastPosX;
        double lastPosY;
        double mousex;
        double mousey;
        double[] originalPositionLeft = new double[9];
        double[] originalPositionTop = new double[9];
        double[] gridPositionLeft = new double[25];
        double[] gridPositionTop = new double[25];
        Grid[] shapes = new Grid[9];
        Rectangle[] gridRectangles = new Rectangle[25];
        Rect[] gridRect = new Rect[25];
        Shape[] shapeobjects;
        int index;
        int freeCells = 0;

        public MainWindow()
        {
            InitializeComponent();
            assignValues();
            getOriginalPosition();
        }

        //Assign Values to grid and shape arrays
        private void assignValues()
        {
            //Assigns Shape objects to array
            shapes = Puzzle.Children.OfType<Grid>().ToArray();

            //Assigns Grid Rectangles to Array
            gridRectangles = Puzzle.Children.OfType<Rectangle>().ToArray();
            
            ///Creates new Rect object inside it's grid rectangle and assigns them to array 
            ///(Rect objects smaller than their parent so they dont trigger intersect with each other)
            for (int i = 0; i < gridRectangles.Length; i++)
            {
                gridRect[i] = new Rect(Canvas.GetLeft(gridRectangles[i]), Canvas.GetTop(gridRectangles[i]), gridRectangles[i].Width, gridRectangles[i].Height);

            }
            shapeobjects = new Shape[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
            {
                shapeobjects[i] = new Shape(shapes[i]);
            }
        }


        private void shape_Loaded(object sender, RoutedEventArgs e)
        {
            getIndex(sender);
            rectupdate((UIElement)sender);
        }

        private void shape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            getIndex(sender);
            // set active element to front
            UIElement shape = shapeobjects[index].shapeobject;
            Panel.SetZIndex((UIElement)sender, 3);
            Point mouse = Mouse.GetPosition(Puzzle);
            mousex = mouse.X - Canvas.GetLeft((UIElement)sender);
            mousey = mouse.Y - Canvas.GetTop((UIElement)sender);
            lastPosition((UIElement)sender);
            _isRectDragInProg = true;
            shape.CaptureMouse();
        }

        private void shape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            UIElement shape = shapeobjects[index].shapeobject;
            _isRectDragInProg = false;
            shape.ReleaseMouseCapture();
            // set inactive element back to position
            Panel.SetZIndex((UIElement)sender, 1);
            snapToGrid((UIElement)sender);
            //setRectangles();
            rectupdate((UIElement)sender);
            resetPosition((UIElement)sender);
            shapeOverlap((UIElement)sender);
            shapesPlaced((UIElement)sender);
            winCondition();
        }

        private void shape_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRectDragInProg) return;
            // get the position of the mouse relative to the Canvas
            var mousePos = e.GetPosition(Puzzle);
            double left = mousePos.X - mousex;
            double top = mousePos.Y - mousey;
            Canvas.SetLeft((UIElement)sender, left);
            Canvas.SetTop((UIElement)sender, top);
        }

        private void getIndex(object sender)
        {
            for (int i = 0; i < shapeobjects.Length; i++)
            {
                if (shapeobjects[i].shapeobject == sender)
                {
                    index = i;
                }
            }
        }
        
        //get initial position of shapes
        private void getOriginalPosition()
        {
            int i = 0;
            foreach (UIElement element in shapes)
            {
                originalPositionLeft[i] = Canvas.GetLeft(element);
                originalPositionTop[i] = Canvas.GetTop(element);
                i += 1;
            }
        }
        
        //Snap to grid 
        private void snapToGrid(UIElement shape)
        {
            // get the search values of the Object
            double valueLeft = Canvas.GetLeft(shape);
            double valueTop = Canvas.GetTop(shape);

            int i = 0;
            foreach (Rectangle rec in gridRectangles)
            {
                gridPositionLeft[i] = Canvas.GetLeft(rec);
                gridPositionTop[i] = Canvas.GetTop(rec);
                i += 1;
            }

            // get the closest left position value to the search value of the Object
            double nearestLeft = gridPositionLeft.Select(p => new { Value = p, Difference = Math.Abs(p - valueLeft) })
                  .OrderBy(p => p.Difference)
                  .First().Value;
            // get the closest top position value to the search value of the Object
            double nearestTop = gridPositionTop.Select(p => new { Value = p, Difference = Math.Abs(p - valueTop) })
                    .OrderBy(p => p.Difference)
                    .First().Value;

            // set object left and top value to nearest top and left grid values if the object is in the grid area
            if (Canvas.GetLeft(shape) >= 380 && Canvas.GetLeft(shape) <= 770 && Canvas.GetTop(shape) >= 100 && Canvas.GetTop(shape) <= 475)
            {
                Canvas.SetLeft(shape, nearestLeft);
                Canvas.SetTop(shape, nearestTop);
            }

        }

        
        // if object is out of window bounds resets it to the original position
        private void resetPosition(UIElement shape)
        {
            if (Canvas.GetLeft(shape) <= -30 || Canvas.GetLeft(shape) >= 1160 || Canvas.GetTop(shape) <= -30 || Canvas.GetTop(shape) >= 600)
            {
                for (int i = 0; i < shapes.Length; i++)
                {
                    if (shape == shapes[i])
                    {
                        Canvas.SetLeft(shape, originalPositionLeft[i]);
                        Canvas.SetTop(shape, originalPositionTop[i]);
                    }
                }
            }
            rectupdate(shape);
        }

        //check if shapes are overlaping and If objects overlap sets the active object to its last position
        private void shapeOverlap(UIElement shape)
        {
            //Checks if shapes overlap
            for (int i = 0; i < shapeobjects[index].rekts.Length; i++)
            {
                for (int n = 0; n < shapeobjects.Length; n++)
                {
                    if (index == n) continue;
                    for (int x = 0; x < shapeobjects[n].rekts.Length; x++)
                    {
                        if (shapeobjects[index].rekts[i].IntersectsWith(shapeobjects[n].rekts[x]))
                        {
                            objectsOverlap = true;
                            break;
                        }
                    }
                }
                if (objectsOverlap == true) break;
            }
            
            //Sets active object to its last position
            if (objectsOverlap == true)
            {
                setLastPosition(shape);
                objectsOverlap = false;
            }
        }

        //Sets the objects position to last position
        private void setLastPosition(UIElement shape)
        {
            Canvas.SetLeft(shape, lastPosX);
            Canvas.SetTop(shape, lastPosY);
            rectupdate(shape);
            shapesPlaced(shape);
            objectsOverlap = false;
        }


        private void shapesPlaced(UIElement shape)
        {
            int n;
            //check if shape rectangle itersects with any grid rectangles
            bool[] intersectCheck = new bool[shapeobjects[index].shapeobject.Children.Count];
            for (int i = 0; i < shapeobjects[index].rekts.Length; i++)
            {
                for (n = 0; n < gridRectangles.Length; n++)
                {
                    if (gridRect[n].IntersectsWith(shapeobjects[index].rekts[i]))
                    {
                        intersectCheck[i] = true;
                    }
                }
            }
            //Check if shape is fully or just partialy placed inside the grid
            if (!intersectCheck.Contains(true) || !intersectCheck.Contains(false))
            {
                validPlacement = true;
            }
            else if (intersectCheck.Contains(true) && intersectCheck.Contains(false))
            {
                validPlacement = false;
            }
            //Check if shape is in or outside of the grid
            if (!intersectCheck.Contains(false))
            {
                shapeobjects[index].insideGrid = true;
            }
            else 
            {
                shapeobjects[index].insideGrid = false;
            }
            //If shape is not fully contained in the grid resets the shape to the last position
            if (validPlacement == false)
            {
                setLastPosition(shape);
            }
            coveredCells();
        }

        private void coveredCells()
        {
            for (int x = 0; x < shapeobjects.Length; x++)
            {
                if (shapeobjects[x].insideGrid == true)
                {
                    freeCells = freeCells + shapeobjects[x].rekts.Count();
                }
                else if (shapeobjects[x].insideGrid == false)
                {
                    freeCells = freeCells - shapeobjects[x].rekts.Count();
                }
            }
        }

        //Gets last position of the object before moving
        private void lastPosition(UIElement shape)
        {
            lastPosX = Canvas.GetLeft(shape);
            lastPosY = Canvas.GetTop(shape);
        }
        
        //Updates the position of active shape rectangles relative to the parent shapes position and Rect objects relative to their parent Rectangle
        private void rectupdate(UIElement shape)
        {
            Point[] shapeRectPosition = new Point[shapeobjects[index].rekts.Length];
            double[] temppositionLeft = new double[shapeobjects[index].rekts.Length];
            double[] temppositionTop = new double[shapeobjects[index].rekts.Length];

            for (int i = 0; i < shapeobjects[index].rekts.Length; i++)
            {
                Vector offset = VisualTreeHelper.GetOffset(shapeobjects[index].rectangles[i]);
                temppositionLeft[i] = Canvas.GetLeft(shape) + offset.X;
                temppositionTop[i] = Canvas.GetTop(shape) + offset.Y;
                shapeRectPosition[i] = new Point { X = temppositionLeft[i] + 1, Y = temppositionTop[i] + 1 };
                shapeobjects[index].rekts[i].Location = shapeRectPosition[i];
                
            }
        }

        //Checks if all grid fields are covered by the shape object
        private void winCondition() 
        {
            if (freeCells == 25)
            {
                if (MessageBox.Show("Congats!!! Do you want to play again?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    reset();
                }
                //Disables shapes from moving after the game is finished
                else
                {
                    for (int i = 0; i < shapes.Length; i++)
                    {
                        shapes[i].IsEnabled = false;
                    }
                }
            }
            else
            {
                freeCells = 0;
            }
        }

        // resets the game
        private void reset()
        {
            for (int i = 0; i < shapes.Length; i++)
            {
                Canvas.SetLeft(shapes[i], originalPositionLeft[i]);
                Canvas.SetTop(shapes[i], originalPositionTop[i]);
                shapeobjects[i].insideGrid = false;
            }
            //Enables shapes to move after the game is restarted (Disabled after the Puzzle is complete)
            for (int i = 0; i < shapes.Length; i++)
            {
                if(shapeobjects[i].shapeobject == shapes[i])
                {
                    index = i;
                }
                shapes[i].IsEnabled = true;
                rectupdate(shapes[i]);
            }
        }
        
        //Exit Game Button
        private void Exit_Game_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Exit Game?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }

        }

        //Reset Button
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to restart?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                reset();
            }
        }

        //Fullscreen button
        private void toggleFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (isFullscreenON == false)
            {
                this.Topmost = true;
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
                this.Topmost = false;
                isFullscreenON = true;
            }
            else if (isFullscreenON == true)
            {
                this.WindowStyle = WindowStyle.ThreeDBorderWindow;
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.NoResize;
                this.Topmost = false;
                isFullscreenON = false;
            }
        }
    }

    public class Shape
    {
        public Grid shapeobject;
        public Rectangle[] rectangles;
        public Rect[] rekts; 
        public bool insideGrid;

        public Shape(Grid shape)
        {
            shapeobject = shape;
            rectangles = shapeobject.Children.OfType<Rectangle>().ToArray();
            rekts = new Rect[rectangles.Length];
            for (int i = 0; i < rectangles.Length; i++)
            {
                rekts[i] = new Rect(Canvas.GetLeft(rectangles[i]) + 1, Canvas.GetTop(rectangles[i]) + 1, rectangles[i].Width - 2, rectangles[i].Height - 2);
            }
        }
    }
}
