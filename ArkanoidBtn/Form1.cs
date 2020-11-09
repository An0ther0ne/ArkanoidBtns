using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArkanoidBtn {
    public partial class MainForm : Form {
        public Game game;
        public MainForm() {
            InitializeComponent();
            game = new Game(ClientSize.Width, ClientSize.Height, this.Controls);
        }
        private void MainForm_MouseMove(object sender, MouseEventArgs e) {
            game.DeskMoveTo(e.X);
        }
        private void MainForm_ResizeEnd(object sender, EventArgs e) {
            game.SetFieldSize(ClientSize.Width, ClientSize.Height);
        }
    }
    public class BaseObj {
        private int _px_, _py_;
        public virtual int PosX { get { return _px_; } set { _px_ = value; } }
        public virtual int PosY { get { return _py_; } set { _py_ = value; } }
        public int Width, Height;
        public BaseObj(int w, int h) {
            Width = w;
            Height = h;
        }
        public Rectangle GetRect() {
            return new Rectangle(PosX, PosY, Width, Height);
        }
        public bool isIntersect(Rectangle a, Rectangle b) {
            int ax2 = a.X + a.Width;
            int ay2 = a.Y + a.Height;
            int bx2 = b.X + b.Width;
            int by2 = b.Y + b.Height;
            if ((bx2 > a.X) && (b.X < ax2) &&
                (by2 > a.Y) && (b.Y < ay2)){
                return true;
            } else {
                return false;
            }
        }
        public bool IntersectWith(Rectangle b) {
            return isIntersect(GetRect(), b);
        }
    }
    public class Field : BaseObj {
        public int FldW, FldH;
        public Field(int w, int h) : base(w, h) {
            NewFieldSize(Width, Height);
        }
        public virtual void NewFieldSize(int w, int h){
            FldW = w;
            FldH = h;
        }
    }
    public class MyBtn : Field{
        public bool Active;
        protected Button body;
        public MyBtn(int w, int h) : base (w, h){
            body = new Button();
            body.Visible = false;
        }
        public void Show() { 
            body.Visible = true;  
            Active = true;
            body.Show(); 
            #if DEBUG
                int maxx = PosX + Width;
                int maxy = PosY + Height;
                body.Text = PosX.ToString() + '-' + maxx.ToString() + "::" + PosY.ToString() + '-' + maxy.ToString();
            #endif
        }
        public void Hide() { 
            body.Visible = false; 
            Active = false;
            body.Hide();
        }
        public void SetLocation(int x, int y) {
            PosX = x;
            PosY = y;
            body.Location = new Point(x, y);
        }
        public void Resize(int w, int h) {
            Width = w;
            Height = h;
            body.Size = new Size(Width, Height);
        }
        public void SetColor(Color c) { body.BackColor = c; }
    }
    public class Game : Field{
        private const int RefreshInterval = 25; // ms
        // private static System.Timers.Timer timer;
        private System.Windows.Forms.Timer timer;
        private static long ticks;

        public int Score = 0;
        public bool GameOver = false;
        
        private const int ballsize = 20;
        private Control.ControlCollection Control;

        private Desk desk;
        private BrickSet bricks;
        private Ball ball;
        public Game(int w, int h, Control.ControlCollection c) : base(w, h){
            Control = c;
            desk = new Desk(Width, Height, Control);
            desk.SetColor(Color.Blue);
            desk.Show();
            bricks = new BrickSet(Width, Height / 2, Control);
            ball = new Ball(Width, Height, desk.Thick + desk.AirBag, ballsize, Control);
            SetTimer();
        }
        ~Game() {
            timer.Stop();
            if (ball != null) {
                ball = null;
            }
            if (desk != null) {
                desk = null;
            }
            if (bricks != null) {
                bricks = null;
            }
            timer.Dispose();
        }
        public void SetFieldSize(int w, int h) {
            NewFieldSize(w, h);
            desk.NewFldSize(w, h);
            bricks.NewFieldSize(w, h / 2);
            ball.NewFieldSize(w, h);
        }
        public void DeskMoveTo(int x){
            if (x > FldW - desk.Width / 2) {
                x = FldW - desk.Width / 2;
            }
            if (x < desk.Width / 2) {
                x = desk.Width / 2;
            }
            desk.SetLocation(x - desk.Width / 2, desk.FldH - desk.Thick - desk.AirBag);
        }
        private void SetTimer() {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = RefreshInterval;
            timer.Tick += new EventHandler(OnTimerTick);
            ticks = DateTime.Now.Ticks;
            timer.Start();
        }
        private void OnTimerTick(Object src, EventArgs e) {
            long t = DateTime.Now.Ticks;
            double dt = (double)(t - ticks) / 1000000;
            ball.Move(dt);
            ticks = t;
            if (CheckCollision()) {
                ball.Move(dt);
            }
            if (bricks.CheckCollisionWith(ball.GetRect())){
                ball.FlipY();
                ball.Move(dt);
            }
            // Console.WriteLine("The Elapsed event was raised at ticks: {0:}", DateTime.Now.Ticks);
        }
        private bool CheckCollision() {
            if (isIntersect(desk.GetRect(), ball.GetRect())) {
                ball.FlipY();
                return true;
            }
            return false;
        }
    }
    public class Desk : MyBtn{
        public int AirBag;
        public int Thick;
        public Desk(int w, int h, Control.ControlCollection c) : base (w, h) {
            SetSize();
            SetLocation((FldW - Width) / 2, FldH - Thick - AirBag);
            c.Add(body);
        }
        private void SetSize() {
            Thick = FldW / 40;
            AirBag = FldW / 50;
            Width = 6 * Thick;
            Height = Thick;
            body.Size = new Size(Width, Height);
        }
        public void NewFldSize(int w, int h){
            PosX = PosX * (w - Width) / (FldW - Width);
            PosY = h - Thick - AirBag;
            base.NewFieldSize(w, h);
            SetSize();
            SetLocation(PosX, PosY);
        }
    }
    public class Brick : MyBtn {
        public Brick(Point pos, int w, int h, Control.ControlCollection c) : base(w, h){
            SetLocation(pos.X, pos.Y);
            body.Size = new Size(Width, Height);
            c.Add(body);
        }
        public Brick(int x, int y, int w, int h, Control.ControlCollection c) : base(w, h) {
            SetLocation(x, y);
            body.Size = new Size(Width, Height);
            c.Add(body);
        }
    }
    public class BrickSet : Field {
        private const int Margins = 10;
        private const int BrksPerW = 8;
        private const int BrksPerH = 8;
        private int Total = 0;
        private Brick[] Bricks;
        public BrickSet(int w, int h, Control.ControlCollection c) : base(w, h){
            Total  = BrksPerW * BrksPerH;
            Bricks = new Brick[Total];
            CalcBrickSize();
            for (int i = 0; i < Total; i++) {
                Bricks[i] = new Brick(CalcPosition(i), Width, Height, c);
                Bricks[i].SetColor(Color.Aqua);
                Bricks[i].Show();
                #if DEBUG
                    Point pos = CalcPosition(i);
                    System.Diagnostics.Debug.WriteLine("I:" + i.ToString() + " --> X=" + pos.X.ToString() + ", Y=" + pos.Y.ToString());
                #endif
            }
        }
        ~BrickSet() {
            for (int i = 0; i < Total; i++) { Bricks[i] = null; }
            Bricks = null;
        }
        private void CalcBrickSize(){
            Width = (FldW - Margins - Margins) / BrksPerW;
            Height = (FldH - Margins - Margins) / BrksPerH;
        }
        public Point CalcPosition(int i) {
            return new Point(
                Margins + Width * (i % BrksPerW),   // x
                Margins + Height * (i / BrksPerW)   // y
        );}
        public override void NewFieldSize(int w, int h){
            base.NewFieldSize(w, h);
            CalcBrickSize();
            for (int i = 0; i < Total; i++) {
                Point newpos = CalcPosition(i);
                Bricks[i].SetLocation(newpos.X, newpos.Y);
                Bricks[i].Resize(Width, Height);
            }
        }
        public bool CheckCollisionWith(Rectangle ballrect) {
            for (int i = 0; i < Total; i++) {
                if (Bricks[i].Active && Bricks[i].IntersectWith(ballrect)) {
                    Bricks[i].Hide();
                    return true;
                }
            }
            return false;
        }
    }
    public class Ball : Brick{
        private double _pxd_, _pyd_;
        public override int PosX { get { return (int)_pxd_; } set { _pxd_ = value; } }
        public override int PosY { get { return (int)_pyd_; } set { _pyd_ = value; } }
        public double SpdX, SpdY;
        public int Size {
            get { return Width; }
            set { Width = Height = value; }
        }
        private int MaxX, MaxY;
        private bool ismove = false;
        private Button btn;

        public Ball(int w, int h, int d, int s, Control.ControlCollection c) : base((w-s)/2, h-d-s, s, s, c){

            FldW = w;
            FldH = h;

            Random rnd = new Random();
            double rv = 4 * (3 + rnd.NextDouble());
            double alpha = Math.PI / 4 + Math.PI * rnd.NextDouble() / 2;
            SpdX = rv * Math.Cos(alpha);
            SpdY = - rv * Math.Sin(alpha);

            btn = new Button();
            btn.Location = new Point((w - s) / 2, h - d - s);
            btn.Size = new Size(s, s);
            btn.BackColor = Color.Brown;
            btn.Show();

            c.Add(btn);
        }
        public void FlipX() {
            SpdX = -SpdX;
        }
        public void FlipY() {
            SpdY = -SpdY;
        }
        public bool CheckX() {
            bool res = false;
            if (PosX + Size > FldW) {
                PosX = FldW - Size;
                res = true;
            }
            if (PosX < 0) {
                PosX = 0;
                res = true;
            }
            return res;
        }
        public bool CheckY() {
            bool res = false;
            if (PosY + Size > FldH) {
                PosY = FldH - Size;
                res = true;
            }
            if (PosY < 0) {
                PosY = 0;
                res = true;
            }
            return res;
        }
        public void Move(double dt) {
            _pxd_ += SpdX * dt;
            _pyd_ += SpdY * dt;
            if (CheckX()) {
                FlipX();
            }
            if (CheckY()) {
                FlipY();
            }
            btn.Location = new Point(PosX, PosY);
        }
    }
}
