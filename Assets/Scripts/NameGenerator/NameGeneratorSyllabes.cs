using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class NameGeneratorSyllabes
{
    public NameGeneratorSyllabes(List<string> list)
    {
        List = list;
    }

    public List<string> List;

    public string GetSyllabe(System.Random random)
    {
        int syllabeIndex = (int)(random.NextDouble() * List.Count);
        return List[syllabeIndex];
    }
}
