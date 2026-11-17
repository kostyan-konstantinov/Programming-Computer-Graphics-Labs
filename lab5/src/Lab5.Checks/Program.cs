using System.Diagnostics;
using System.Globalization;
using Lab5.Core;

CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
if(args.Contains("--benchmark"))
{
    int at=Array.IndexOf(args,"--benchmark");
    string path=at+1<args.Length?args[at+1]:"report/local";
    var progress=new Progress<string>(text=>Console.WriteLine(text));
    Benchmark.Save(Benchmark.Run(progress:progress),Path.GetFullPath(path));
    Console.WriteLine("Saved: "+Path.GetFullPath(path));return;
}
int assertions=0;
var watch=Stopwatch.StartNew();
void Check(bool condition,string name)
{
    assertions++;if(!condition)throw new Exception("FAIL: "+name);
}
var w=new Box(-10,-10,10,10);
var cases=new (string Name,Segment Input,bool Visible,Segment Expected)[]
{
    ("inside",new(new(-2,3),new(8,4)),true,new(new(-2,3),new(8,4))),
    ("outside",new(new(-20,-20),new(-11,-11)),false,default),
    ("left boundary",new(new(-10,-20),new(-10,20)),true,new(new(-10,-10),new(-10,10))),
    ("top boundary",new(new(-20,10),new(20,10)),true,new(new(-10,10),new(10,10))),
    ("bottom boundary",new(new(-20,-10),new(20,-10)),true,new(new(-10,-10),new(10,-10))),
    ("right boundary",new(new(10,-20),new(10,20)),true,new(new(10,-10),new(10,10))),
    ("corner point",new(new(10,10),new(10,10)),true,new(new(10,10),new(10,10))),
    ("point outside",new(new(11,10),new(11,10)),false,default),
    ("parallel outside",new(new(-20,10.01),new(20,10.01)),false,default),
    ("corner tangent",new(new(-20,0),new(0,20)),true,new(new(-10,10),new(-10,10))),
    ("diagonal",new(new(-20,-20),new(20,20)),true,new(new(-10,-10),new(10,10))),
    ("vertical",new(new(0,-20),new(0,20)),true,new(new(0,-10),new(0,10)))
};
foreach(var method in Enum.GetValues<LineMethod>())foreach(var c in cases)
{
    bool ok=LineClipper.Clip(c.Input,w,method,out var result);
    Check(ok==c.Visible,$"{method}/{c.Name}/visibility");
    if(ok)Check((result.A-c.Expected.A).Length<5e-7&&(result.B-c.Expected.B).Length<5e-7,$"{method}/{c.Name}/coordinates");
}
Console.WriteLine("PASS lines: all boundaries, zero-length corner, parallel, tangent, diagonal");
var random=new Random(21);
for(int i=0;i<12000;i++)
{
    var s=new Segment(new(random.NextDouble()*100-50,random.NextDouble()*100-50),new(random.NextDouble()*100-50,random.NextDouble()*100-50));
    bool a=LineClipper.Liang(s,w,out var la),b=LineClipper.Cohen(s,w,out var co),c=LineClipper.Midpoint(s,w,out var mi);
    Check(a==b&&b==c,"random line visibility "+i);
    if(a)Check((la.A-co.A).Length<1e-6&&(la.B-co.B).Length<1e-6&&(la.A-mi.A).Length<1e-6&&(la.B-mi.B).Length<1e-6,"random line coordinates "+i+$" input={s} liang={la} cohen={co} midpoint={mi}");
}
Console.WriteLine("PASS 12000 differential line cases");
foreach(string type in new[]{"dense","short"})
{
    var data=Samples.Generate(500000,type);
    var tree=new QuadTree(data,Samples.World,7);
    var ids=new List<int>();
    for(int step=0;step<12;step++)
    {
        double x=random.NextDouble()*1600-800,y=random.NextDouble()*1600-800;
        var window=new Box(x-120,y-90,x+120,y+90);
        tree.Query(window,ids);
        var indexed=ids.Where(id=>LineClipper.Liang(data[id],window,out _)).Order().ToArray();
        var full=Enumerable.Range(0,data.Length).Where(id=>LineClipper.Liang(data[id],window,out _)).ToArray();
        Check(indexed.SequenceEqual(full),"500k tree/full scan "+type+step);
        foreach(var method in Enum.GetValues<LineMethod>())
        {
            var clipped=LineClipper.ClipAll(data,tree,window,method);
            Check(clipped.Segments.Length==full.Length,"500k all methods count");
            Check(clipped.Segments.All(s=>window.Contains(s.A)&&window.Contains(s.B)),"500k visible coordinates");
        }
    }
    Check(LineClipper.ClipAll(data,tree,Samples.World,LineMethod.Midpoint).Segments.Length==500000,"500k full visible result");
    Console.WriteLine("PASS 500000 "+type+": 12 moved windows, all methods, full scan oracle");
}
foreach(int invalid in new[]{0,-1,500001})
{
    bool rejected=false;
    try{Samples.Generate(invalid,"dense");}catch(ArgumentOutOfRangeException){rejected=true;}
    Check(rejected,"generator 500k limit");
}
Vec[] Rect(double x1,double y1,double x2,double y2)=>new Box(x1,y1,x2,y2).Ring;
var polygonCases=new (string Name,List<Vec[]> Subject,List<Vec[]> Clip,double Area,int Parts,int Holes)[]
{
    ("rectangle",[Rect(-20,-20,20,20)],[w.Ring],400,1,0),
    ("disjoint",[Rect(20,20,30,30)],[w.Ring],0,0,0),
    ("identical",[w.Ring],[w.Ring],400,1,0),
    ("hole",[Rect(-20,-20,20,20),Rect(-5,-5,5,5)],[w.Ring],300,1,1),
    ("island inside hole",[Rect(-20,-20,20,20),Rect(-5,-5,5,5),Rect(-2,-2,2,2)],[w.Ring],316,2,1),
    ("bowtie",[new Vec[]{new(-10,-10),new(10,10),new(-10,10),new(10,-10)}],[Rect(-20,-20,20,20)],200,2,0),
    ("touching edge",[Rect(10,-5,20,5)],[w.Ring],0,0,0),
    ("through corners",[new Vec[]{new(-20,0),new(0,-20),new(20,0),new(0,20)}],[w.Ring],400,1,0),
    ("clipper hole",[Rect(-20,-20,20,20)],[w.Ring,Rect(-5,-5,5,5)],300,1,1),
    ("duplicate rings cancel",[w.Ring,w.Ring],[w.Ring],0,0,0),
    ("split U",[new Vec[]{new(-9,-9),new(9,-9),new(9,9),new(3,9),new(3,-3),new(-3,-3),new(-3,9),new(-9,9)}],[Rect(-10,0,10,10)],108,2,0)
};
foreach(var c in polygonCases)
{
    var result=PolygonClipper.Clip(c.Subject,c.Clip);
    Check(Math.Abs(result.Area-c.Area)<1e-5,$"polygon/{c.Name}/area {result.Area}");
    Check(result.Components==c.Parts&&result.Holes==c.Holes,$"polygon/{c.Name}/topology {result.Components}/{result.Holes}");
}
Console.WriteLine("PASS polygon exact areas/topology: holes, islands, split, self-intersection, overlap, tangency");
void CheckMembership(List<Vec[]> subject,List<Vec[]> clip,string name)
{
    var result=PolygonClipper.Clip(subject,clip);
    for(int k=0;k<1000;k++)
    {
        var p=new Vec(random.NextDouble()*200-100,random.NextDouble()*200-100);
        Check(Geo.Inside(p,result.Rings)==(Geo.Inside(p,subject)&&Geo.Inside(p,clip)),"polygon membership "+name+"/"+k);
    }
}
for(int i=0;i<100;i++)
{
    var points=Enumerable.Range(0,12).Select(_=>new Vec(random.NextDouble()*160-80,random.NextDouble()*160-80)).ToArray();
    CheckMembership([points],[Rect(-45,-35,45,35)],"random self-intersecting "+i);
}
for(int i=0;i<60;i++)
{
    Vec[] Shape()=>Enumerable.Range(0,9).Select(_=>new Vec(random.NextDouble()*160-80,random.NextDouble()*160-80)).ToArray();
    CheckMembership([Shape(),Rect(-10,-10,10,10)],[Shape()],"arbitrary clipper "+i);
}
string? samples=args.Length>0?args[0]:null;
if(samples!=null)
{
    foreach(string file in Directory.GetFiles(Path.Combine(samples,"polygons"),"*.txt"))
    {
        var sample=PolygonSample.Load(file);
        CheckMembership(sample.Rings,[sample.Window.Ring],Path.GetFileName(file));
        string temporary=Path.Combine(Path.GetTempPath(),"lab5-"+Guid.NewGuid()+".txt");
        try
        {
            sample.Save(temporary);
            var restored=PolygonSample.Load(temporary);
            Check(restored.Window==sample.Window&&restored.Name==sample.Name,"TXT window and name round trip");
            Check(restored.Rings.Count==sample.Rings.Count,"TXT ring count round trip");
            for(int i=0;i<sample.Rings.Count;i++)
                Check(restored.Rings[i].SequenceEqual(sample.Rings[i]),"TXT exact coordinates round trip");
        }
        finally {File.Delete(temporary);}
    }
    foreach(string file in Directory.GetFiles(Path.Combine(samples,"segments"),"*.txt"))
    {
        var sample=Samples.LoadLines(file);
        foreach(var s in sample.Lines)
        {
            bool expected=LineClipper.Liang(s,sample.Window,out _);
            foreach(var method in Enum.GetValues<LineMethod>()) Check(LineClipper.Clip(s,sample.Window,method,out _)==expected,"sample "+file+method);
        }
    }
}
{
    string temporary=Path.Combine(Path.GetTempPath(),"lab5-"+Guid.NewGuid()+".txt");
    try
    {
        new PolygonSample {Name="Пустой результат",Window=w,Rings=[]}.Save(temporary);
        Check(PolygonSample.Load(temporary).Rings.Count==0,"TXT empty polygon result");
        foreach(string invalid in new[]{"", "-1", "10000", "1\n2\n0 0\n1 1\n-10 -10 10 10",
            "1\n3\n0 0\n1 0\n0 NaN\n-10 -10 10 10", "0\n0 0 0 10",
            "0\n-10 -10 10 10 extra", "1\n2001", "1\n3\n0 0\n1 1", "0\n-10 -10 Infinity 10"})
        {
            File.WriteAllText(temporary,invalid);
            bool rejected=false;
            try {PolygonSample.Load(temporary);} catch(FormatException) {rejected=true;}
            Check(rejected,"TXT malformed input");
        }
    }
    finally {File.Delete(temporary);}
}
Console.WriteLine("PASS TXT import/export, empty result, invalid counts, missing coordinates, non-finite values");
{
    var summary=Benchmark.Summarize(new double[]{9,1,7,3,5});
    Check(Math.Abs(summary.Median-5)<1e-12&&Math.Abs(summary.P10-1.8)<1e-12&&Math.Abs(summary.P90-8.2)<1e-12,"benchmark known median and percentiles");
    Check(Benchmark.Summarize(new[]{.125})==new TimingStats(.125,.125,.125),"benchmark singleton series");
    foreach(var invalid in new[]{Array.Empty<double>(),new[]{double.NaN},new[]{double.PositiveInfinity},new[]{-1.0}})
    {
        bool rejected=false;
        try{Benchmark.Summarize(invalid);}catch(ArgumentException){rejected=true;}
        Check(rejected,"benchmark invalid timing");
    }
    using var cancelled=new CancellationTokenSource();cancelled.Cancel();
    bool stopped=false;
    try{Benchmark.Run(10,cancellation:cancelled.Token);}catch(OperationCanceledException){stopped=true;}
    Check(stopped,"benchmark cancellation before measurements");
}
{
    string directory=Path.Combine(Path.GetTempPath(),"lab5-csv-"+Guid.NewGuid());
    var data=new BenchmarkResult();
    var timing=new TimingStats(.125,.0625,.25);
    data.Rows.Add(new("Плотные",500000,6,100,timing,timing,timing,1234.5));
    data.Density.Add(new("Короткие",100000,timing,250));
    data.Algorithms.Add(new("Средняя точка",timing,100,200,300));
    data.Samples.Add(new("Глубина","Плотные",500000,6,"Отсечение",1,8,.125));
    try
    {
        foreach(string culture in new[]{"en-US","ru-RU"})
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            Benchmark.Save(data,directory);var copy=Benchmark.Load(directory);
            Check(copy.Rows.SequenceEqual(data.Rows)&&copy.Density.SequenceEqual(data.Density)&&copy.Algorithms.SequenceEqual(data.Algorithms)&&copy.Samples.SequenceEqual(data.Samples),"benchmark Russian CSV exact round trip "+culture);
            foreach(string file in Directory.GetFiles(directory,"*.csv"))
            {
                byte[] bytes=File.ReadAllBytes(file);
                Check(bytes.Take(3).SequenceEqual(new byte[]{0xef,0xbb,0xbf}),"benchmark CSV UTF-8 BOM "+culture);
                string header=File.ReadLines(file).First();
                Check(header.Contains(';')&&header.Any(c=>c>='А'&&c<='я'),"benchmark CSV Russian headers "+culture);
            }
            Check(File.ReadAllText(Path.Combine(directory,"benchmark.csv")).Contains("0,125"),"benchmark CSV Russian decimal comma "+culture);
            Check(Directory.GetFiles(directory).Length==4&&Directory.GetFiles(directory).All(f=>Path.GetExtension(f)==".csv"),"benchmark saves only CSV "+culture);
        }
        foreach(string name in new[]{"benchmark.csv","density.csv","algorithms.csv","samples.csv"})
        {
            string isolated=Path.Combine(directory,"Отдельный CSV");Directory.CreateDirectory(isolated);
            string selected=Path.Combine(isolated,"Мои замеры.csv");File.Copy(Path.Combine(directory,name),selected,true);
            var single=Benchmark.Load(selected);
            Check(single.HasResults,"benchmark opens a single renamed CSV without sidecar files: "+name);
            Check(single.Rows.Count+single.Density.Count+single.Algorithms.Count+single.Samples.Count==1,"benchmark reads selected CSV data: "+name);
            Directory.Delete(isolated,true);
        }
    }
    finally {CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;Directory.Delete(directory,true);}
}
Console.WriteLine($"PASS {assertions:N0} assertions in {watch.Elapsed.TotalSeconds:F2} s");
