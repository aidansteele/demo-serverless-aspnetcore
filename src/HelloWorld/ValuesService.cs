using System.Collections.Generic;

namespace HelloWorld
{
    public interface IValuesService
    {
        IEnumerable<string> GetValues();
    }

    public class ValuesService : IValuesService
    {
        public IEnumerable<string> GetValues()
        {
            return new List<string>
            {
                "value1",
                "value2"
            };
        }
    }
}