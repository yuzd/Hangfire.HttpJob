using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Util
{
    /// <summary>
    /// Non-generic version of <see cref="IEnumerable"/> wrapper.
    /// </summary>
    internal class ProgressEnumerable : IEnumerable
    {
        private readonly IEnumerable _enumerable;
        private readonly IProgressBar _progressBar;
        private readonly int _count;

        public ProgressEnumerable(IEnumerable enumerable, IProgressBar progressBar, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _count = count;
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count);
        }

        private class Enumerator : IEnumerator, IDisposable
        {
            private readonly IEnumerator _enumerator;
            private readonly IProgressBar _progressBar;
            private int _count, _index;

            public Enumerator(IEnumerator enumerator, IProgressBar progressBar, int count)
            {
                _enumerator = enumerator;
                _progressBar = progressBar;
                _count = count;
                _index = -1;
            }

            public object Current => _enumerator.Current;

            public void Dispose()
            {
                try
                {
                    (_enumerator as IDisposable)?.Dispose();
                }
                finally
                {
                    _progressBar.SetValue(100);
                }
            }

            public bool MoveNext()
            {
                var r = _enumerator.MoveNext();
                if (r)
                {
                    _index++;

                    if (_index >= _count)
                    {
                        // adjust maxCount if overrunned
                        _count = _index + 1;
                    }

                    _progressBar.SetValue(_index * 100.0 / _count);
                }
                return r;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }
        }
    }

    /// <summary>
    /// Generic version of <see cref="IEnumerable{T}"/> wrapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ProgressEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;
        private readonly IProgressBar _progressBar;
        private readonly int _count;

        public ProgressEnumerable(IEnumerable<T> enumerable, IProgressBar progressBar, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            _progressBar = progressBar ?? throw new ArgumentNullException(nameof(progressBar));
            _count = count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count);
        }

        private class Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;
            private readonly IProgressBar _progressBar;
            private int _count, _index;

            public Enumerator(IEnumerator<T> enumerator, IProgressBar progressBar, int count)
            {
                _enumerator = enumerator;
                _progressBar = progressBar;
                _count = count;
                _index = -1;
            }

            public T Current => _enumerator.Current;

            object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

            public void Dispose()
            {
                try
                {
                    _enumerator.Dispose();
                }
                finally
                {
                    _progressBar.SetValue(100);
                }
            }

            public bool MoveNext()
            {
                var r = _enumerator.MoveNext();
                if (r)
                {
                    _index++;

                    if (_index >= _count)
                    {
                        // adjust maxCount if overrunned
                        _count = _index + 1;
                    }

                    _progressBar.SetValue(_index * 100.0 / _count);
                }
                return r;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }
        }
    }
}
