using System.Collections.Generic;
using System.Linq;

namespace ZoomMuteStatus
{
    public class TreeNode<T>
    {
        public TreeNode(T root)
        {
            Current = root;
        }

        public T Current { get; }
        public List<TreeNode<T>> Nodes { get; set; } = new List<TreeNode<T>>();

        public int Count()
        {
            return 1 + Nodes.Sum(t => t.Count());
        }

        public List<T> GetElements()
        {
            var result = new List<T>();
            result.Add(Current);
            result.AddRange(Nodes.SelectMany(n => n.GetElements()));

            return result;
        }
    }
}