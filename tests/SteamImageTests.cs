using System;
using System.IO;
using MuseDashSteamPresence;
class SteamImageTests
{
    static int checks;
    static void Check(bool value,string message) { checks++; if (!value) throw new Exception(message); }
    static void Put(byte[] b,int p,uint n) { Buffer.BlockCopy(BitConverter.GetBytes(n),0,b,p,4); }
    static byte[] Fixture()
    {
        var b=new byte[1024];b[0]=0x4d;b[1]=0x5a;Put(b,60,128);Put(b,128,0x4550);
        b[132]=0x64;b[133]=0x86;b[134]=1;b[148]=240;b[152]=0x0b;b[153]=2;
        Put(b,212,512);Put(b,260,16);Put(b,408,512);Put(b,412,512);
        for (int i=512;i<b.Length;i++) b[i]=(byte)(i%251);
        return b;
    }
    static void Main()
    {
        var b=Fixture();var expected=SteamImageFingerprint.Compute(b);
        Check(expected!=null,"Valid PE rejected");
        // Every mapped byte, import/export directory and section header matters.
        for (int i=0;i<b.Length;i++)
        {
            var changed=(byte[])b.Clone();changed[i]^=1;
            bool ignored=(i>=136 && i<140)||(i>=216 && i<220);
            Check((SteamImageFingerprint.Compute(changed)==expected)==ignored,"Unexpected ignored byte "+i);
        }
        var signed=new byte[b.Length+16];Buffer.BlockCopy(b,0,signed,0,b.Length);
        Put(signed,296,1024);Put(signed,300,16);Put(signed,1024,16);signed[1029]=2;signed[1030]=2;
        Check(SteamImageFingerprint.Compute(signed)==expected,"Certificate normalization failed");
        signed[1032]=42;Check(SteamImageFingerprint.Compute(signed)==expected,"Signature data changed fingerprint");
        Put(signed,296,512);Check(SteamImageFingerprint.Compute(signed)==null,"Mapped certificate overlap accepted");
        for (int length=0;length<1024;length+=7)
        { var cut=new byte[length];Buffer.BlockCopy(b,0,cut,0,length);Check(SteamImageFingerprint.Compute(cut)==null,"Truncated file accepted"); }
        var random=new Random(774171);
        for (int i=0;i<1000;i++)
        {
            var corrupt=(byte[])b.Clone();int offset=random.Next(64,512);Put(corrupt,offset,(uint)random.Next());
            // Malformed PE input must never escape as an exception.
            SteamImageFingerprint.Compute(corrupt);
        }
        bool equivalent;
        Check(SteamBuild.Identify(b,out equivalent)==null,"Unknown valid PE accepted");
        string path=Environment.GetEnvironmentVariable("MUSE_STEAM_AUDIT_DLL");
        if (!String.IsNullOrEmpty(path))
        {
            var actual=File.ReadAllBytes(path);
            Check(SteamBuild.Identify(actual,out equivalent)!=null && !equivalent,"Known DLL rejected");
            int pe=(int)BitConverter.ToUInt32(actual,60);
            actual[pe+8]^=1;
            Check(SteamBuild.Identify(actual,out equivalent)!=null && equivalent,"Equivalent real DLL rejected");
            int section=pe+24+BitConverter.ToUInt16(actual,pe+20);
            int code=(int)BitConverter.ToUInt32(actual,section+20);
            actual[code+32]^=1;
            Check(SteamBuild.Identify(actual,out equivalent)==null,"Changed real code accepted");
            Console.WriteLine("PASS: real DLL exact/equivalent/code-change cases, in-memory only; no native load or Steam call.");
        }
        Console.WriteLine("PASS: "+checks+" image checks plus 1000 malformed-header probes; no Steam calls.");
    }
}
