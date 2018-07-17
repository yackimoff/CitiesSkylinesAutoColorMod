using System.Collections;
using System.Collections.Generic;

namespace AutoLineColor.Util
{
    internal class ReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly IList<T> _list;

        public ReadOnlyList(IList<T> list)
        {
            _list = list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int Count => _list.Count;

        public T this[int index] => _list[index];
    }
}
