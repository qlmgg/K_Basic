﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility.通用;

namespace Utility.任务
{
    public class H任务队列
    {
        private ConcurrentDictionary<string, M队列> _队列缓存 = new ConcurrentDictionary<string, M队列>();

        private H队列监控 _监控;

        /// <param name="__延迟阈值">毫秒</param>
        /// <param name="__耗时阈值">毫秒</param>
        public H任务队列(int __分组统计数量 = 1000, int __延迟阈值 = 3000, int __耗时阈值 = 100)
        {
            _监控 = new H队列监控(__分组统计数量, __延迟阈值, __耗时阈值);
        }

        public void 添加事项<T>(string __队列标识, T __数据, Action<T> __处理数据, bool __监控 = false)
        {
            var __队列 = _队列缓存.GetOrAdd(__队列标识, k => new M队列(__队列标识));
            if (__监控)
            {
                __队列.添加事项(__数据, __处理数据, _监控);
            }
            else
            {
                __队列.添加事项(__数据, __处理数据);
            }
        }

        public void 关闭队列(string __队列标识)
        {
            M队列 __队列;
            if (_队列缓存.TryRemove(__队列标识, out __队列))
            {
                __队列.关闭();
            }
        }

        public void 关闭所有()
        {
            _队列缓存.Values.ToList().ForEach(q => q.关闭());
        }

        public void 取消队列(string __线程标识)
        {
            M队列 __队列;
            if (_队列缓存.TryRemove(__线程标识, out __队列))
            {
                __队列.取消();
            }
        }

        public void 取消所有()
        {
            _队列缓存.Values.ToList().ForEach(q => q.取消());
        }

        class M队列
        {
            private Task _任务 = Task.Factory.StartNew(() => { });

            private CancellationTokenSource _取消标志 = new CancellationTokenSource();

            private ConcurrentQueue<Action> _队列 = new ConcurrentQueue<Action>();

            private string _名称;

            private bool _已关闭;

            private AutoResetEvent _同步信号 = new AutoResetEvent(false);

            public M队列(string __名称)
            {
                _名称 = __名称;
            }

            public void 添加事项<T>(T __数据, Action<T> __处理数据, H队列监控 __监控 = null)
            {
                //Debug.WriteLine("{0} 添加事项 {1}", DateTime.Now.ToString("HH:mm:ss.fff"), __数据);
                if (_已关闭)
                {
                    return;
                }
                var __接收时间 = Environment.TickCount;
                Action __任务项 = () =>
                {
                    try
                    {
                        //Debug.WriteLine("{0} 执行事项 {1}", DateTime.Now.ToString("HH:mm:ss.fff"), __数据);
                        if (__监控 == null)
                        {
                            __处理数据(__数据);
                        }
                        else
                        {
                            __监控.监控下执行(_名称, __数据, __接收时间, __处理数据);
                        }
                    }
                    catch (Exception ex)
                    {
                        H调试.记录异常(ex, _名称);
                    }
                };
                _队列.Enqueue(__任务项);
                if (_队列.Count == 1)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Action __事项;
                        while (_队列.TryDequeue(out __事项))
                        {
                            if (_取消标志.IsCancellationRequested)
                            {
                                break;
                            }
                            __事项();
                        }
                        if (_已关闭)
                        {
                            _同步信号.Set();
                            return;
                        }
                    }, _取消标志.Token);
                }
            }

            public void 关闭()
            {
                _已关闭 = true;
                if (_队列.Count != 0)
                {
                    _同步信号.WaitOne();
                }
            }

            public void 取消()
            {
                _取消标志.Cancel();
                _同步信号.Set();
            }
        }
    }
}
