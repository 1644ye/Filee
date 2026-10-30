using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;
using Filee.App.Services;
using B=Comparison.Before;
using A=Comparison.After;
class Program {
 [STAThread] public static void Main(string[] args)=>AppBuilder.Configure<Demo>().UsePlatformDetect().With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Software] }).StartWithClassicDesktopLifetime(args);
}
class Demo:Application {
 public override void OnFrameworkInitializationCompleted(){
 if(ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime life) life.MainWindow=new DemoWindow();
 base.OnFrameworkInitializationCompleted();
 }
}
class DemoWindow:Window {
 readonly B.DonutMenu b=new(){Width=410,Height=410,OuterRadius=155,CanFlip=true,CenterTitle="Filee",CenterHint="Tab to flip",CornerRadius=new(8)};
 readonly A.DonutMenu a=new(){Width=410,Height=410,OuterRadius=155,CanFlip=true,CenterTitle="Filee",CenterHint="Tab to flip",CornerRadius=new(8)};
 readonly TextBlock status=new(){FontSize=20,HorizontalAlignment=HorizontalAlignment.Center};
 int bf,af;
 public DemoWindow(){
 Title="Filee PR 815 - Native Windows animation comparison";Width=920;Height=590;CanResize=false;Background=new SolidColorBrush(Color.Parse("#e9edf3"));
 b.Items=BeforeItems();a.Items=AfterItems();
 b.FaceSwapping+=(_,_)=>{bf++;b.Items=BeforeItems();};a.FaceSwapping+=(_,_)=>{af++;a.Items=AfterItems();};
 var columns=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Spacing=25};
 columns.Children.Add(Panel("BEFORE - upstream 79b227d",b));columns.Children.Add(Panel("AFTER - PR #815 eaaea17",a));
 Content=new StackPanel{Margin=new(20),Spacing=12,Children={new TextBlock{Text="Native Windows / Skia software | identical timing / nine slices",FontSize=20,HorizontalAlignment=HorizontalAlignment.Center},columns,status}};
 Opened+=async(_,_)=>{await Task.Delay(2500);while(IsVisible){await Cycle();}};
 }
 StackPanel Panel(string label,Control c)=>new(){Children={new TextBlock{Text=label,FontSize=19,HorizontalAlignment=HorizontalAlignment.Center},c}};
 static string[] Front=["PNG","JPEG","WebP","SVG","PDF","TIFF","BMP","AVIF","ICO"];
 static string[] Back=["Small","Medium","Large","Original","Square","Portrait","Landscape","Archive","Share"];
 B.DonutItem[] BeforeItems()=>(bf%2==0?Front:Back).Select((s,i)=>new B.DonutItem{Id=i.ToString(),Label=s}).ToArray();
 A.DonutItem[] AfterItems()=>(af%2==0?Front:Back).Select((s,i)=>new A.DonutItem{Id=i.ToString(),Label=s}).ToArray();
 async Task Cycle(){
 Motion.Set(true);status.Text="Open (normal speed)";b.PlayOpenAnimation();a.PlayOpenAnimation();await Task.Delay(1800);
 status.Text="Flip";b.Flip();a.Flip();await Task.Delay(1500);
 status.Text="Two rapid flips (80 ms apart)";b.Flip();a.Flip();await Task.Delay(80);b.Flip();a.Flip();await Task.Delay(1700);
 status.Text="Close";b.PlayCloseAnimation(()=>{});a.PlayCloseAnimation(()=>{});await Task.Delay(1000);
 status.Text="Close interrupted opening (100 ms)";b.PlayOpenAnimation();a.PlayOpenAnimation();await Task.Delay(100);b.PlayCloseAnimation(()=>{});a.PlayCloseAnimation(()=>{});await Task.Delay(1700);
 status.Text="Reduced motion: open and flip";Motion.Set(false);b.PlayOpenAnimation();a.PlayOpenAnimation();await Task.Delay(800);b.Flip();a.Flip();await Task.Delay(1600);
 }
}

