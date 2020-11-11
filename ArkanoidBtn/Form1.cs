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
        public int isIntersect(Rectangle a, Rectangle b) {
            int ax2 = a.X + a.Width;
            int ay2 = a.Y + a.Height;
            int bx2 = b.X + b.Width;
            int by2 = b.Y + b.Height;
            if ((bx2 > a.X) && (b.X < ax2) && (by2 > a.Y) && (b.Y < ay2)){
                if (Delta(a.Y, ay2, b.Y, by2) > Delta(a.X, ax2, b.X, bx2)){
                    return 1;
                }else{
                    return 2;
                }
            } else {
                return 0;
            }
        }
        private int Delta(int a1, int a2, int b1, int b2){
            if (b1 < a1){
                if (b2 > a1){
                    return b2 - a1;
                }else{
                    return 0;
                }
            }
            if (b2 > a2) {
                if (b1 < a2) {
                    return a2 - b1;
                } else {
                    return 0;
                }
            }
            if ((b1 > a1) && (b2 < a2)){
                return b2 - b1;
            }
            return 0;
        }
        public int IntersectWith(Rectangle b) {
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
        public Button Body { get { return _body_; } }
        private Button _body_;
        public MyBtn(int w, int h) : base (w, h){
            _body_ = new Button();
            Body.Visible = false;
        }
        public void Show() { 
            _body_.Visible = true;  
            Active = true;
            _body_.Show(); 
            #if DEBUG
                int maxx = PosX + Width;
                int maxy = PosY + Height;
                _body_.Text = PosX.ToString() + '-' + maxx.ToString() + "::" + PosY.ToString() + '-' + maxy.ToString();
            #endif
        }
        public void Hide() { 
            _body_.Visible = false; 
            Active = false;
            _body_.Hide();
        }
        public void SetLocation(int x, int y) {
            PosX = x;
            PosY = y;
            _body_.Location = new Point(x, y);
        }
        public void Resize(int w, int h) {
            Width = w;
            Height = h;
            _body_.Size = new Size(Width, Height);
        }
        public void Resize(int s) {
            Width = Height = s;
            _body_.Size = new Size(s, s);
        }
        public void SetColor(Color c) { _body_.BackColor = c; }
    }
    public class Game : Field{
        private const int RefreshInterval = 25;         // ms
        private System.Windows.Forms.Timer timer;
        private static long ticks;

        public int Score = 0;
        public bool GameOver = false;
        
        private Control.ControlCollection Control;

        private Desk desk;
        private BrickSet bricks;
        private Ball ball;
        public Game(int w, int h, Control.ControlCollection c) : base(w, h){
            Control = c;
            desk = new Desk(Width, Height, Control);
            desk.SetColor(Color.Blue);
            desk.Body.MouseMove += new MouseEventHandler(MouseOnDeskMove);
            desk.Show();
            bricks = new BrickSet(Width, Height / 2, Control);
            ball = new Ball(Width, Height, desk.Thick + desk.AirBag, Control);
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
        public void DeskMoveRel(int x) {
            DeskMoveTo(desk.PosX + x);
        }
        private void MouseOnDeskMove(object src, MouseEventArgs e) {
            DeskMoveRel(e.X);
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
            int col_res = bricks.CheckCollisionWith(ball.GetRect());
            if (col_res == 0) {
                col_res = isIntersect(desk.GetRect(), ball.GetRect());
            }
            if (col_res > 0) {
                ProceedCollision(col_res);
                ball.Move(dt);
            }
        }
        private void ProceedCollision(int r) {
            switch (r) {
                case 1:
                    ball.FlipX();
                    break;
                case 2:
                    ball.FlipY();
                    break;
            }
        }
    }
    public class Desk : MyBtn{
        public int AirBag;
        public int Thick;
        public Desk(int w, int h, Control.ControlCollection c) : base (w, h) {
            SetSize();
            SetLocation((FldW - Width) / 2, FldH - Thick - AirBag);
            c.Add(Body);
        }
        private void SetSize() {
            Thick = FldW / 40;
            AirBag = FldW / 50;
            Width = 6 * Thick;
            Height = Thick;
            Body.Size = new Size(Width, Height);
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
        public Brick(int x, int y, Control.ControlCollection c) : base(CalcSize(x, y), CalcSize(x, y)) {
            Init(x, y, CalcSize(x, y), CalcSize(x, y), c);
        }
        public Brick(Rectangle r, Control.ControlCollection c) : base(r.Width, r.Height) {
            Init(r.X, r.Y, r.Width, r.Height, c);
        }
        public Brick(Point pos, int w, int h, Control.ControlCollection c) : base(w, h){
            Init(pos.X, pos.Y, w, h, c);
        }
        public Brick(int x, int y, int w, int h, Control.ControlCollection c) : base(w, h) {
            Init(x, y, w, h, c);
        }
        protected static int CalcSize(int w, int h){
            return Math.Min(w, h) / 26;
        }
        private void Init(int x, int y, int w, int h, Control.ControlCollection c) {
            SetLocation(x, y);
            Body.Size = new Size(w, h);
            c.Add(Body);
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
                #if DEBUG
                    if (i % BrksPerW == BrksPerW / 2) {
                        continue;
                    }
                    if (i % BrksPerW == BrksPerW / 2 + 1) {
                        continue;
                    }
                #endif
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
                #if DEBUG
                    if (i % BrksPerW == BrksPerW / 2) {
                        continue;
                    }
                    if (i % BrksPerW == BrksPerW / 2 + 1) {
                        continue;
                    }
                #endif
                Point newpos = CalcPosition(i);
                Bricks[i].SetLocation(newpos.X, newpos.Y);
                Bricks[i].Resize(Width, Height);
            }
        }
        public int CheckCollisionWith(Rectangle ballrect) {
            for (int i = 0; i < Total; i++) {
                if (Bricks[i] != null && Bricks[i].Active ) {
                    int res = Bricks[i].IntersectWith(ballrect);
                    if (res > 0) {
                        Bricks[i].Hide();
                        return res;
                    }
                }
            }
            return 0;
        }
    }
    public class Ball : Brick{
        private double _pxd_, _pyd_;
        public override int PosX { get { return (int)_pxd_; } set { _pxd_ = value; } }
        public override int PosY { get { return (int)_pyd_; } set { _pyd_ = value; } }
        public double SpdX, SpdY;
        public int Size { get { return Width; } set { Width = Height = value; } }
        public Ball(int w, int h, int d, Control.ControlCollection c) : base(w/2, h-d, c){

            FldW = w;
            FldH = h;
            Random rnd = new Random();
            double rv = 8 * (3 + rnd.NextDouble());
            double alpha = Math.PI / 8 + Math.PI * rnd.NextDouble() / 4;

            if (rnd.Next(2) > 0) {
                alpha = -alpha;
            } 

            SpdX = rv * Math.Cos(alpha);
            SpdY = - rv * Math.Sin(alpha);

            PosX = Body.Location.X - Size / 2;
            PosY = Body.Location.Y - Size;

            Body.Location = new Point(PosX, PosY);
            Body.BackColor = Color.Brown;
            Body.FlatStyle = FlatStyle.Flat;
            Body.Show();
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
            Body.Location = new Point(PosX, PosY);
        }
        public void NewFieldSize(int w, int h){     // Not override! 
            SpdX *= (double) w / FldW;
            SpdY *= (double) h / FldH;
            base.NewFieldSize(w, h);
            Resize(Size = CalcSize(w / 2, h));
        }
    }
}
