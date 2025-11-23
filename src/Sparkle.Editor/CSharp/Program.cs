using Sparkle.Editor.EditorRuntime;
public class Program
{
    public static void Main(string[] args)
    {
        EditorInstance editor = new EditorInstance();
        editor.Setup();
        editor.Run(); 
    }
}