using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace Comet.Game.World.Schedule
{
    public sealed class SchedulerFactory
    {
        private readonly StdSchedulerFactory mFactory;
        private IScheduler mScheduler;

        public SchedulerFactory()
        {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

            mFactory = new StdSchedulerFactory();
        }

        public async Task StartAsync()
        {
            mScheduler = await mFactory.GetScheduler();
            await mScheduler.Start();
        }

        public async Task StopAsync()
        {
            await mScheduler.Shutdown();
        }

        public async Task ScheduleAsync<T>(string cron) where T : IJob
        {
            string name = typeof(T).Name;
            var key = new JobKey(name);
            IJobDetail job = JobBuilder.Create<AutomaticActionJob>()
                                       .WithIdentity(key)
                                       .Build();

            ITrigger trigger = TriggerBuilder.Create()
                                             .WithIdentity(name)
                                             .StartNow()
                                             .WithCronSchedule(cron)
                                             .Build();

            await mScheduler.ScheduleJob(job, trigger);
        }

        private class ConsoleLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= LogLevel.Info && func != null)
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level,-10}] - {func()}", parameters);
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                return new DisposableDummy();
                //throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                //throw new NotImplementedException();
                return new DisposableDummy();
            }

            private class DisposableDummy : IDisposable
            {
                void IDisposable.Dispose()
                {
                }
            }
        }
    }
}