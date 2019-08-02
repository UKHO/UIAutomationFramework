namespace UKHO.SpecflowSessionDependencyInjection
{
    public interface ISessionFactory
    {
        BaseSession CreateSession(string testTitle);
    }
}