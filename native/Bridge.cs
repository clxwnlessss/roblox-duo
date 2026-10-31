using System;
using System.Diagnostics;
using System.Threading;
class Bridge {
 static Mutex first, second;
 static void Release() { foreach (Mutex m in new Mutex[]{second,first}) if(m!=null){try{m.ReleaseMutex();}catch(ApplicationException){} m.Dispose();} first=second=null; }
 static Mutex Own(string name){bool created; var m=new Mutex(true,name,out created);if(!created){m.Dispose();throw new Exception("Session lock is busy. Close Roblox and other multi-instance utilities, then retry.");}return m;}
 static int Count(){var ps=Process.GetProcessesByName("RobloxPlayerBeta");int n=ps.Length;foreach(var p in ps)p.Dispose();return n;}
 static void Main(string[] args){string prefix=""; Guid testId; if(args.Length==1 && Guid.TryParse(args[0],out testId))prefix="DuoTest-"+testId.ToString()+"-"; try{string line;while((line=Console.ReadLine())!=null){try{if(line=="count") Console.WriteLine("COUNT:"+Count());else if(line=="enable"){if(first!=null){Console.WriteLine("OK");continue;}if(Count()>0)throw new Exception("Close all Roblox windows before enabling Duo mode.");try{first=Own(prefix+"ROBLOX_singletonMutex");second=Own(prefix+"ROBLOX_singletonEvent");}catch{Release();throw;}Console.WriteLine("OK");}else if(line=="disable"){Release();Console.WriteLine("OK");}else if(line=="quit")break;else throw new Exception("Unknown command.");}catch(Exception ex){Console.WriteLine("ERROR:"+ex.Message);}Console.Out.Flush();}}finally{Release();}}
}
