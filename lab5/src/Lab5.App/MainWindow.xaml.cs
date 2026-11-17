using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Lab5.Core;
using LibTessDotNet;
using Microsoft.Win32;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using Vec = Lab5.Core.Vec;

namespace Lab5.App;

public partial class MainWindow : Window
{
    readonly SceneRenderer renderer = new();
    readonly List<Segment> visible = [];
    readonly List<string> lineFiles = [], polygonFiles = [];
    Segment[] lines = [];
    List<Vec[]> polygon = [];
    PolygonResult polygonResult = new([],0,0);
    QuadTree? tree;
    Box lineWindow = new(-40,-30,40,30), polygonWindow = new(-40,-30,40,30), world = Samples.World;
    Vec lineCamera, polygonCamera;
    double lineScale=4, polygonScale=4;
    bool polygonMode, ready, dirty=true, uploadOriginal=true, working;
    bool redraw=true, uploadResult=true, fullRedraw=true;
    Task<LineResult>? pendingLines;
    Box pendingWindow, shownWindow, paintedWindow;
    int dataVersion, pendingVersion, lastFramebuffer, lastWidth, lastHeight;
    bool dragging, panning;
    int editLine=-1, editEnd, editRing=-1, editVertex=-1;
    Vec lastMouse;
    double clipMs;
    BenchmarkResult? benchmarkResults;
    float[] polygonFill=[], polygonOutline=[], originalOutline=[];
    string samplesPath="";
    Box WindowBox { get => polygonMode ? polygonWindow : lineWindow; set { if(polygonMode) polygonWindow=value; else lineWindow=value; } }
    Vec Camera { get => polygonMode ? polygonCamera : lineCamera; set { if(polygonMode) polygonCamera=value; else lineCamera=value; } }
    double Scale { get => polygonMode ? polygonScale : lineScale; set { if(polygonMode) polygonScale=value; else lineScale=value; } }
    LineMethod Method => (LineMethod)Math.Max(0,Algorithm.SelectedIndex);

    public MainWindow()
    {
        InitializeComponent();
        samplesPath=FindSamples();
        if(Directory.Exists(Path.Combine(samplesPath,"segments")))
            lineFiles.AddRange(Directory.GetFiles(Path.Combine(samplesPath,"segments"),"*.txt").Order());
        if(Directory.Exists(Path.Combine(samplesPath,"polygons")))
            polygonFiles.AddRange(Directory.GetFiles(Path.Combine(samplesPath,"polygons"),"*.txt").Order());
        foreach(var file in lineFiles) LineSample.Items.Add(Path.GetFileNameWithoutExtension(file));
        foreach(var file in polygonFiles) PolygonSampleBox.Items.Add(PolygonSample.Load(file));
        ready=true;
        if(lineFiles.Count>0) LineSample.SelectedIndex=0;
        else LoadLines([new(new(-80,-45),new(90,50)),new(new(-60,30),new(60,30))],lineWindow);
        if(polygonFiles.Count>0) PolygonSampleBox.SelectedIndex=0;
        Viewport.Start(new GLWpfControlSettings {MajorVersion=3,MinorVersion=3,RenderContinuously=true,UseDeviceDpi=false});
        Loaded+=(_,_)=>Fit();
        Viewport.SizeChanged+=(_,_)=>ViewChanged();
        Viewport.IsVisibleChanged+=(_,e)=> {if((bool)e.NewValue)redraw=fullRedraw=true;};
        Viewport.LostMouseCapture+=(_,_)=> {dragging=panning=false;};
    }
    static string FindSamples()
    {
        foreach(string start in new[]{AppContext.BaseDirectory,Environment.CurrentDirectory})
        {
            var d=new DirectoryInfo(start);
            for(int i=0;d!=null&&i<9;i++,d=d.Parent)
                if(Directory.Exists(Path.Combine(d.FullName,"samples","polygons"))) return Path.Combine(d.FullName,"samples");
        }
        return Path.Combine(AppContext.BaseDirectory,"samples");
    }
    void LoadLines(Segment[] data,Box window)
    {
        lines=data; lineWindow=window; world=Samples.Bounds(lines);
        tree=new(lines,world,ReadDepth()); dataVersion++;visible.Clear();
        uploadOriginal=uploadResult=dirty=redraw=fullRedraw=true;shownWindow=window;
        if(!polygonMode) { SyncWindowInputs(); Fit(); }
        Status.Text=$"Загружено {lines.Length:N0} отрезков • Quadtree: {tree.Nodes:N0} узлов";
    }
    int ReadDepth()
    {
        if(!int.TryParse(DepthInput.Text,out int depth)||depth<1||depth>12) throw new FormatException("Глубина дерева: целое число от 1 до 12.");
        return depth;
    }
    void LoadPolygon(PolygonSample sample)
    {
        polygon=sample.Rings.Select(r=>r.ToArray()).ToList();polygonWindow=sample.Window;dataVersion++;
        originalOutline=SceneRenderer.Outlines(polygon);polygonOutline=[];polygonFill=[];
        polygonResult=new([],0,0);shownWindow=polygonWindow;fullRedraw=true;
        if(polygonMode) { SyncWindowInputs(); Fit(); }
        dirty=true;
    }
    void SetMode(bool polygons)
    {
        if(working) return;
        polygonMode=polygons;dataVersion++;shownWindow=WindowBox;
        LineControls.Visibility=polygons?Visibility.Collapsed:Visibility.Visible;
        PolygonControls.Visibility=polygons?Visibility.Visible:Visibility.Collapsed;
        ShowLinePoints.Visibility=polygons?Visibility.Collapsed:Visibility.Visible;
        PolygonDisplayControls.Visibility=polygons?Visibility.Visible:Visibility.Collapsed;
        dirty=redraw=true; SyncWindowInputs();ViewChanged();
        if(polygons && polygonScale==4) Fit();
    }
    void ModeTabs_Changed(object sender,SelectionChangedEventArgs e)
    { if(ready && e.Source==ModeTabs) SetMode(ModeTabs.SelectedIndex==1); }
    void LineSetting_Changed(object sender,RoutedEventArgs e) { if(ready) {dataVersion++;dirty=true;} }
    void VisualSetting_Changed(object sender,RoutedEventArgs e) { if(ready) redraw=fullRedraw=true; }
    void LineSample_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||LineSample.SelectedIndex<0) return;
        Safe(()=>{ var sample=Samples.LoadLines(lineFiles[LineSample.SelectedIndex]);
            LoadLines(sample.Lines,sample.Window); });
    }
    void PolygonSample_Changed(object sender,SelectionChangedEventArgs e)
    { if(ready&&PolygonSampleBox.SelectedItem is PolygonSample sample) Safe(()=>LoadPolygon(sample)); }
    void OpenLines_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog { Filter="Отрезки (*.txt)|*.txt",InitialDirectory=Path.Combine(samplesPath,"segments") };
        if(dialog.ShowDialog()==true) Safe(()=>{var s=Samples.LoadLines(dialog.FileName);LoadLines(s.Lines,s.Window);});
    }
    void OpenPolygon_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog { Filter="Многоугольники (*.txt)|*.txt",InitialDirectory=Path.Combine(samplesPath,"polygons") };
        if(dialog.ShowDialog()==true) Safe(()=>{var s=PolygonSample.Load(dialog.FileName);LoadPolygon(s);});
    }
    async void Generate_Click(object sender,RoutedEventArgs e)
    {
        if(working) return;
        if(!int.TryParse(CountInput.Text,out int count)||count<1||count>Samples.MaxLines) { Status.Text="Количество: от 1 до 500 000.";return; }
        int depth;
        try { depth=ReadDepth(); } catch(Exception ex) { Status.Text=ex.Message; return; }
        string distribution=Distribution.SelectedIndex==0?"dense":"short";
        SetBusy(true); GenerateButton.IsEnabled=false; Status.Text="Создание данных и построение Quadtree…";
        try
        {
            var sw=Stopwatch.StartNew();
            var result=await Task.Run(()=>{var data=Samples.Generate(count,distribution);return (data,index:new QuadTree(data,Samples.World,depth));});
            lines=result.data; tree=result.index; world=Samples.World;dataVersion++;visible.Clear();
            lineWindow=new(-120,-90,120,90);shownWindow=lineWindow;
            lineCamera=default; lineScale=Math.Min(Math.Max(1,Viewport.ActualWidth)/2200,Math.Max(1,Viewport.ActualHeight)/2200);
            uploadOriginal=uploadResult=dirty=redraw=fullRedraw=true;ViewChanged();
            SyncWindowInputs(); Status.Text=$"Создание данных и индекса: {sw.Elapsed.TotalMilliseconds:0.00} мс; узлов: {tree.Nodes:N0}.";
        }
        catch(Exception ex) { Status.Text=ex.Message; }
        finally { SetBusy(false); GenerateButton.IsEnabled=true; }
    }
    async void Rebuild_Click(object sender,RoutedEventArgs e)
    {
        if(working) return;
        int depth;
        try { depth=ReadDepth(); } catch(Exception ex) { Status.Text=ex.Message;return; }
        SetBusy(true); Status.Text="Построение индекса…";
        try
        {
            var sw=Stopwatch.StartNew();
            tree=await Task.Run(()=>new QuadTree(lines,world,depth));dataVersion++;dirty=true;
            Status.Text=$"Построение дерева: {sw.Elapsed.TotalMilliseconds:0.00} мс; узлов: {tree.Nodes:N0}.";
        }
        catch(Exception ex) { Status.Text=ex.Message; }
        finally { SetBusy(false); }
    }
    void SyncWindowInputs() { WindowWidth.Text=WindowBox.Width.ToString("0.####",CultureInfo.InvariantCulture);WindowHeight.Text=WindowBox.Height.ToString("0.####",CultureInfo.InvariantCulture); }
    void Resize_Click(object sender,RoutedEventArgs e)=>Safe(()=>
    {
        double Read(string text)=>double.Parse(text.Replace(',','.'),CultureInfo.InvariantCulture);
        double width=Read(WindowWidth.Text),height=Read(WindowHeight.Text);
        if(!double.IsFinite(width)||!double.IsFinite(height)||width<1e-4||height<1e-4||width>2e6||height>2e6) throw new FormatException("Размеры окна: от 0.0001 до 2 000 000.");
        Vec c=WindowBox.Center; WindowBox=new(c.X-width/2,c.Y-height/2,c.X+width/2,c.Y+height/2);dirty=true;
    });
    void Fit_Click(object sender,RoutedEventArgs e)=>Fit();
    void Fit()
    {
        IEnumerable<Vec> points=polygonMode?polygon.SelectMany(r=>r):lines.SelectMany(s=>new[]{s.A,s.B});
        Box b;
        if(!polygonMode) b=world;
        else
        {
            var p=points.ToArray();
            b=p.Length>0?new(p.Min(v=>v.X),p.Min(v=>v.Y),p.Max(v=>v.X),p.Max(v=>v.Y)):WindowBox;
        }
        b=new(Math.Min(b.Left,WindowBox.Left),Math.Min(b.Bottom,WindowBox.Bottom),Math.Max(b.Right,WindowBox.Right),Math.Max(b.Top,WindowBox.Top));
        Camera=b.Center;
        double padding=Math.Max(b.Width,b.Height)*.15+Geo.Eps*10;
        Scale=Math.Clamp(Math.Min(Math.Max(400,Viewport.ActualWidth)/Math.Max(b.Width+padding,1e-5),Math.Max(400,Viewport.ActualHeight)/Math.Max(b.Height+padding,1e-5))*.80,1e-5,1e5);
        ViewChanged();
    }
    Vec ScreenToWorld(Point p)=>new(Camera.X+(p.X-Viewport.ActualWidth/2)/Scale,Camera.Y-(p.Y-Viewport.ActualHeight/2)/Scale);
    void Viewport_MouseDown(object sender,MouseButtonEventArgs e)
    {
        if(working) return;
        lastMouse=ScreenToWorld(e.GetPosition(Viewport));
        panning=e.ChangedButton==MouseButton.Right;
        dragging=!panning;
        editLine=editRing=-1;
        if(dragging&&(Keyboard.Modifiers&ModifierKeys.Control)!=0)
        {
            double best=10/Scale;
            if(polygonMode)
            {
                for(int r=0;r<polygon.Count;r++) for(int v=0;v<polygon[r].Length;v++)
                { double d=(polygon[r][v]-lastMouse).Length;if(d<best) {best=d;editRing=r;editVertex=v;} }
            }
            else if(lines.Length<=10000)
                for(int i=0;i<lines.Length;i++) for(int end=0;end<2;end++)
                { double d=((end==0?lines[i].A:lines[i].B)-lastMouse).Length;if(d<best) {best=d;editLine=i;editEnd=end;} }
        }
        dragging=dragging&&(editLine>=0||editRing>=0||WindowBox.Contains(lastMouse));
        if(dragging||panning) {Viewport.CaptureMouse();e.Handled=true;}
    }
    void Viewport_MouseUp(object sender,MouseButtonEventArgs e) { dragging=panning=false;Viewport.ReleaseMouseCapture(); }
    void Viewport_MouseMove(object sender,MouseEventArgs e)
    {
        if(!dragging&&!panning) return;
        if((dragging&&e.LeftButton!=MouseButtonState.Pressed)||(panning&&e.RightButton!=MouseButtonState.Pressed))
        {dragging=panning=false;Viewport.ReleaseMouseCapture();return;}
        Vec p=ScreenToWorld(e.GetPosition(Viewport)),delta=p-lastMouse;
        if(panning) {Camera-=delta;ViewChanged();}
        else if(editRing>=0) {polygon[editRing][editVertex]=p;dataVersion++;dirty=fullRedraw=true;}
        else if(editLine>=0)
        {
            var copy=(Segment[])lines.Clone();var s=copy[editLine];copy[editLine]=editEnd==0?new(p,s.B):new(s.A,p);
            lines=copy;world=Samples.Bounds(copy);tree=new(copy,world,ReadDepth());dataVersion++;
            uploadOriginal=dirty=fullRedraw=true;
        }
        else { WindowBox=WindowBox.Move(delta);dirty=true; }
        lastMouse=ScreenToWorld(e.GetPosition(Viewport));
    }
    void Viewport_MouseWheel(object sender,MouseWheelEventArgs e)
    {
        var p=e.GetPosition(Viewport); Vec before=ScreenToWorld(p);
        Scale=Math.Clamp(Scale*Math.Pow(1.15,e.Delta/120.0),1e-5,1e5);
        Camera+=before-ScreenToWorld(p);ViewChanged();e.Handled=true;
    }
    void Render(TimeSpan delta)
    {
        if(!ready||Viewport.ActualWidth<1||Viewport.ActualHeight<1)return;
        if(pendingLines?.IsCompleted==true)
        {
            try
            {
                var result=pendingLines.GetAwaiter().GetResult();
                if(pendingVersion==dataVersion&&!polygonMode)
                {
                    visible.Clear();visible.AddRange(result.Segments);
                    clipMs=result.Milliseconds;shownWindow=pendingWindow;uploadResult=redraw=true;
                    StatsText.Text=$"Всего: {lines.Length:N0}; кандидаты: {result.Candidates:N0}; видимые: {visible.Count:N0}";
                    TimingText.Text=$"Отсечение: {clipMs:0.00} мс";
                }
            }
            catch(Exception ex){Status.Text=ex.Message;}
            pendingLines=null;
        }
        if(dirty&&!working)
        {
            if(!polygonMode&&lines.Length>10000)
            {
                if(pendingLines==null)
                {
                    var data=lines;var index=UseTree.IsChecked==true?tree:null;var window=lineWindow;var method=Method;
                    pendingWindow=window;pendingVersion=dataVersion;dirty=false;
                    pendingLines=Task.Run(()=>LineClipper.ClipAll(data,index,window,method));
                }
            }
            else
            {
                try{Recalculate();}catch(Exception ex){Status.Text=ex.Message;}
                dirty=false;redraw=true;
            }
        }
        bool surfaceChanged=lastFramebuffer!=Viewport.Framebuffer||lastWidth!=Viewport.FrameBufferWidth||lastHeight!=Viewport.FrameBufferHeight;
        if(surfaceChanged)
        {
            lastFramebuffer=Viewport.Framebuffer;lastWidth=Viewport.FrameBufferWidth;lastHeight=Viewport.FrameBufferHeight;
            redraw=fullRedraw=true;
        }
        if(!redraw)return;
        redraw=false;
        if(!fullRedraw&&!polygonMode&&lines.Length>10000)
        {
            var area=new Box(Math.Min(paintedWindow.Left,shownWindow.Left),Math.Min(paintedWindow.Bottom,shownWindow.Bottom),
                Math.Max(paintedWindow.Right,shownWindow.Right),Math.Max(paintedWindow.Top,shownWindow.Top));
            renderer.RestrictArea(area,Camera,Scale*lastWidth/Viewport.ActualWidth,Scale*lastHeight/Viewport.ActualHeight,lastWidth,lastHeight);
        }
        else GL.Disable(EnableCap.ScissorTest);
        renderer.Begin(Camera,Viewport.ActualWidth/(2*Scale),Viewport.ActualHeight/(2*Scale));
        if(uploadOriginal&&!polygonMode){renderer.SetOriginal(lines);uploadOriginal=false;}
        if(uploadResult&&!polygonMode){renderer.SetResult(visible);uploadResult=false;}
        if(ShowOriginal.IsChecked==true)
        {
            if(polygonMode)renderer.Draw(originalOutline,PrimitiveType.Lines,.5f,.5f,.5f);
            else renderer.DrawOriginal();
        }
        DrawGrid();
        if(polygonMode)
        {
            if(ShowFill.IsChecked==true)renderer.Draw(polygonFill,PrimitiveType.Triangles,.65f,.84f,1,.55f);
            renderer.Draw(polygonOutline,PrimitiveType.Lines,0,.32f,.85f);
            if(ShowVertices.IsChecked==true)
            {
                GL.PointSize(5);
                renderer.Draw(polygon.SelectMany(r=>r).SelectMany(v=>new[]{(float)v.X,(float)v.Y}).ToArray(),PrimitiveType.Points,.5f,.5f,.5f);
                renderer.Draw(polygonResult.Rings.SelectMany(r=>r).SelectMany(v=>new[]{(float)v.X,(float)v.Y}).ToArray(),PrimitiveType.Points,0,.32f,.85f);
            }
        }
        else
        {
            renderer.DrawResult();
            if(ShowLinePoints.IsChecked==true&&lines.Length<1000){GL.PointSize(5);renderer.Draw(SceneRenderer.Lines(visible),PrimitiveType.Points,0,.32f,.85f);}
        }
        renderer.Draw(SceneRenderer.Outlines([shownWindow.Ring]),PrimitiveType.Lines,.92f,.25f,.12f);
        GL.Disable(EnableCap.ScissorTest);paintedWindow=shownWindow;fullRedraw=false;
    }
    void Recalculate()
    {
        var sw=Stopwatch.StartNew();
        if(polygonMode)
        {
            polygonResult=PolygonClipper.Clip(polygon,[polygonWindow.Ring]);clipMs=sw.Elapsed.TotalMilliseconds;
            polygonOutline=SceneRenderer.Outlines(polygonResult.Rings);originalOutline=SceneRenderer.Outlines(polygon);
            polygonFill=Tessellate(polygonResult.Rings);
            StatsText.Text=$"Части: {polygonResult.Components}; отверстия: {polygonResult.Holes}; площадь: {polygonResult.Area:0.####}";
        }
        else
        {
            var result=LineClipper.ClipAll(lines,UseTree.IsChecked==true?tree:null,lineWindow,Method);
            visible.Clear();visible.AddRange(result.Segments);clipMs=result.Milliseconds;uploadResult=true;
            StatsText.Text=$"Всего: {lines.Length:N0}; кандидаты: {result.Candidates:N0}; видимые: {visible.Count:N0}";
        }
        shownWindow=WindowBox;TimingText.Text=$"Отсечение: {clipMs:0.00} мс";
    }
    static float[] Tessellate(List<Vec[]> rings)
    {
        if(rings.Count==0) return [];
        var tess=new Tess();
        foreach(var ring in rings) tess.AddContour(ring.Select(p=>new ContourVertex {Position=new Vec3((float)p.X,(float)p.Y,0)}).ToArray());
        tess.Tessellate(WindingRule.EvenOdd,ElementType.Polygons,3);
        var vertices=new List<float>();
        for(int i=0;i<tess.ElementCount*3;i++)
        {
            int index=tess.Elements[i];if(index==Tess.Undef) continue;
            vertices.Add(tess.Vertices[index].Position.X);vertices.Add(tess.Vertices[index].Position.Y);
        }
        return vertices.ToArray();
    }
    double GridStep()
    {
        double raw=80/Scale,p=Math.Pow(10,Math.Floor(Math.Log10(raw))),n=raw/p;
        return (n<2?2:n<5?5:10)*p;
    }
    void DrawGrid()
    {
        double hx=Viewport.ActualWidth/(2*Scale),hy=Viewport.ActualHeight/(2*Scale),step=GridStep();
        var grid=new List<Segment>();
        for(double x=Math.Floor((Camera.X-hx)/step)*step;x<=Camera.X+hx;x+=step) grid.Add(new(new(x,Camera.Y-hy),new(x,Camera.Y+hy)));
        for(double y=Math.Floor((Camera.Y-hy)/step)*step;y<=Camera.Y+hy;y+=step) grid.Add(new(new(Camera.X-hx,y),new(Camera.X+hx,y)));
        renderer.Draw(SceneRenderer.Lines(grid),PrimitiveType.Lines,.87f,.87f,.87f,.65f);
        renderer.Draw(SceneRenderer.Lines(new[]{new Segment(new(Camera.X-hx,0),new(Camera.X+hx,0)),new Segment(new(0,Camera.Y-hy),new(0,Camera.Y+hy))}),PrimitiveType.Lines,.6f,.6f,.6f);
    }
    void ViewChanged()
    {
        redraw=fullRedraw=true;
        if(!ready||Viewport.ActualWidth<60||Viewport.ActualHeight<60)return;
        UpdateLabels();
    }
    void UpdateLabels()
    {
        AxisLabels.Children.Clear();double w=Viewport.ActualWidth,h=Viewport.ActualHeight,step=GridStep();
        void Label(string text,double x,double y)
        {
            var block=new TextBlock {Text=text,FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(110,110,110))};
            Canvas.SetLeft(block,x);Canvas.SetTop(block,y);AxisLabels.Children.Add(block);
        }
        for(double x=Math.Floor((Camera.X-w/(2*Scale))/step)*step;x<=Camera.X+w/(2*Scale);x+=step)
            Label(x.ToString("G5",CultureInfo.InvariantCulture),w/2+(x-Camera.X)*Scale+4,Math.Clamp(h/2+Camera.Y*Scale+5,4,h-22));
        for(double y=Math.Floor((Camera.Y-h/(2*Scale))/step)*step;y<=Camera.Y+h/(2*Scale);y+=step)
            Label(y.ToString("G5",CultureInfo.InvariantCulture),Math.Clamp(w/2-Camera.X*Scale+5,5,w-50),h/2-(y-Camera.Y)*Scale+3);
        Label("X",w-20,Math.Clamp(h/2+Camera.Y*Scale-20,4,h-24));Label("Y",Math.Clamp(w/2-Camera.X*Scale+8,8,w-24),8);
    }
    void Benchmark_Click(object sender,RoutedEventArgs e)=>Safe(()=>
    {
        if(!int.TryParse(CountInput.Text,out int n)||n<1||n>Samples.MaxLines)throw new FormatException("Количество: от 1 до 500 000.");
        var dialog=new BenchmarkWindow(Path.GetFullPath(Path.Combine(samplesPath,"..","report")),n,benchmarkResults){Owner=this};
        dialog.ShowDialog();benchmarkResults=dialog.Results;
    });
    void Export_Click(object sender,RoutedEventArgs e)=>Safe(()=>
    {
        if(dirty||pendingLines!=null){Status.Text="Дождитесь расчёта.";return;}
        var dialog=new SaveFileDialog{Filter="TXT (*.txt)|*.txt",FileName=polygonMode?"polygon-result.txt":"segments-result.txt"};
        if(dialog.ShowDialog()!=true)return;
        if(polygonMode)
        {
            var sample=new PolygonSample {Name="Результат",Window=shownWindow,Rings=polygonResult.Rings};
            sample.Save(dialog.FileName);
        }
        else
        {
            var sb=new StringBuilder();sb.AppendLine(visible.Count.ToString(CultureInfo.InvariantCulture));
            foreach(var s in visible)sb.AppendLine(FormattableString.Invariant($"{s.A.X:R} {s.A.Y:R} {s.B.X:R} {s.B.Y:R}"));
            sb.AppendLine(FormattableString.Invariant($"{lineWindow.Left:R} {lineWindow.Bottom:R} {lineWindow.Right:R} {lineWindow.Top:R}"));
            File.WriteAllText(dialog.FileName,sb.ToString());
        }

        Status.Text="Сохранено.";
    });
    void SetBusy(bool busy)
    {
        working=busy;
        LineControls.IsEnabled=PolygonControls.IsEnabled=!busy;
        ModeTabs.IsEnabled=!busy;
    }
    void Safe(Action action)
    {
        try{action();}catch(Exception ex){Status.Text=ex.Message;MessageBox.Show(ex.Message,"lab5",MessageBoxButton.OK,MessageBoxImage.Information);}
    }
}
