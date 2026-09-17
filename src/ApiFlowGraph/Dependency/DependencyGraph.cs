public sealed class DependencyGraph
{
    private readonly Dictionary<string, List<string>> _dependencyList;
    private readonly Stack<string> sequence;
    public DependencyGraph()
    {
      _dependencyList=new Dictionary<string, List<string>>();
      sequence=new Stack<string>();
    }
    public void AddDependency(string task, string prerequisite)
    {
        if(!_dependencyList.Keys.Contains(task))
        _dependencyList[task]=new List<string>();

        if(!_dependencyList.Keys.Contains(prerequisite))
        _dependencyList[prerequisite]=new List<string>();

        _dependencyList[task].Add(prerequisite);

    }
  //a->b,c
  //b->c
  //c->d
    private bool HasCycle()
    {
        HashSet<string> visited=new HashSet<string>();
        HashSet<string> visiting =new HashSet<string>();
       foreach(var task in _dependencyList.Keys)
        {
            if(!visited.Contains(task))
            {
                if(HasCycleUtil(task, visiting,visited))
                return true;
            }
        }
        return false;
    }
    private bool HasCycleUtil(string task, HashSet<string> visiting, HashSet<string>visited)
    {
        //a->b
        if(visiting.Contains(task))
        return false;
        if(visited.Contains(task))
        return true;
       
        visiting.Add(task);

        foreach( var neighbour in _dependencyList[task])
        {//HasCycleUtil(d)
            HasCycleUtil(neighbour, visiting, visited);
        }

        visiting.Remove(task);
        //d,c,b,a
        visited.Add(task);
        sequence.Push(task);
        return false;
    }

    public List<string> GetSequence()
    {
        List<string> order=new List<string>();
        if(HasCycle())
          return order;
        if(sequence.Count>0)
        {
            while(sequence.Count!=0)
            order.Add(sequence.Pop());
        }
        order.Reverse();
        return order;
    }
}