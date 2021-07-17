#define uselongtask
#nullable enable
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaobaoProductPhotosSpider
{
    /// <summary>
    /// 在非UI线程中模拟 <see cref="CoreDispatcher"/>
    /// </summary>
    class NoUIDispatcher : IDisposable
    {
#if DEBUG
        ~NoUIDispatcher()
        {
            System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " bedeleted");
        }
#endif
        bool isstarted = false;
#pragma warning disable VSTHRD100
        public async void Start()
#pragma warning restore VSTHRD100
        {
            if (isstarted)
                return;
            isstarted = true;
            using (od = new ManualResetEvent(false))
            {
                normaltasklist = new Queue<ValueTuple<Action, TaskCompletionSource<Exception?>?>>();
#if uselongtask
                await Task.Factory.StartNew(() =>
#else
                await Task.Run(() =>
#endif
                {
                    try
                    {
                        while (isdispose == false)
                        {
                            lock (normaltasklist)
                            {
                                if (normaltasklist.Count == 0)
                                {
                                    od.Reset();
                                }
                                else
                                {
                                    var thistask = normaltasklist.Dequeue();
                                    try
                                    {
                                        thistask.Item1.Invoke();
                                        thistask.Item2?.SetResult(null);
                                    }
                                    catch (Exception e)
                                    {
                                        System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + "  report error" + e.ToString());
                                        thistask.Item2?.SetResult(e);
                                    }
                                }
                            }
                            od.WaitOne();
                        }
                        normaltasklist = null;
                        System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " dispose");
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " error");
                    }
#if uselongtask
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
#else
                });
#endif
            }
        }
        ManualResetEvent? od;
        Queue<ValueTuple<Action, TaskCompletionSource<Exception?>?>>? normaltasklist = null;

        public async Task RunAsync(Action agileCallback)
        {
            var pk = new TaskCompletionSource<Exception?>();
            if (normaltasklist == null || od == null)
            {
                System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " isdisposed");
                return;
            }
            lock (normaltasklist)//锁不会等待太久
            {
                normaltasklist.Enqueue(new ValueTuple<Action, TaskCompletionSource<Exception?>?>(agileCallback, pk));
            }
            od.Set();
            var ex = await pk.Task;
            if (ex != null)
            {
                throw ex;
            }
        }
        public void Run(Action agileCallback)
        {
            Task.Run(() =>
            {
                try
                {
                    if (normaltasklist == null || od == null)
                    {
                        System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " isdisposed");
                        return;
                    }
                    lock (normaltasklist)
                    {
                        normaltasklist.Enqueue(new ValueTuple<Action, TaskCompletionSource<Exception?>?>(agileCallback, null));
                    }
                    od.Set();
                }
#if DEBUG
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(nameof(NoUIDispatcher) + " " + e.ToString());
                }
#else
                catch { }
#endif
            }).Forget();
        }
        bool isdispose = false;
        public void Dispose()
        {
            isdispose = true;
            od?.Set();
        }
    }
}
