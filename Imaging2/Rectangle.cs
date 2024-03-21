namespace Imaging2
{
    public class Rectangle
    {
        public Rectangle()
        {

        }
        public Rectangle(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }
}