public abstract class Prompt<T>
{
    public abstract string GetSystemPrompt();
    public abstract string GetUserPrompt(T input);
}