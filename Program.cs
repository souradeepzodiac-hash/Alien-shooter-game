namespace VoidHunter;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            GameApp.Run(args);
        }
        catch (Exception ex)
        {
            try
            {
                string log = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(log, ex.ToString());
                Console.Error.WriteLine(ex);
            }
            catch { /* last resort */ }
            throw;
        }
    }
}
