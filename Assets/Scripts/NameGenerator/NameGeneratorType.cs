using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class NameGeneratorType
{
    public enum Types
    {
        World,
        Kingdom,
        Town
    };

    public string TypeName;
    public Types Type;
    public List<NameGeneratorSyllabes> Syllabes;

    public string GetName(System.Random random)
    {
        string name = "";
        foreach (NameGeneratorSyllabes nameGeneratorSyllabes in Syllabes)
        {
            name += nameGeneratorSyllabes.GetSyllabe(random);
        }
        return name;
    }
}
