using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPipeLine
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime start = DateTime.Now;

            List<ILogic> logics = new List<ILogic>();

            for (int i = 1; i <= 100; i++)
            {
                var logic = new Logic1(string.Format("{0:d2}", i));
                logics.Add(logic);
                logic.execute();
            }

            var terminate = false;
            while (!terminate)
            {
                terminate = IsTerminate(logics);
                Thread.Sleep(1);
            }

            Console.WriteLine("Main end. [{0}/s]", DateTime.Now - start);
        }

        public static bool IsTerminate(List<ILogic> logics)
        {
            bool terminate = true;
            foreach (var logic in logics)
            {
                terminate &= logic.Terminate;
            }
            return terminate;
        }

    }

    public interface ILogic
    {
        string Name { get; set; }
        bool Terminate { get; set; }
        void execute();
        void notifyEnd();
    }

    class Logic1 : ILogic
    {
        public string[] Files { get; set; }
        public string Name { get; set; }
        public bool Terminate { get; set; }
        public TestPipeFilter StartFilter { get; set; }

        public Logic1(string name)
        {
            Name = name;
            Terminate = false;

            TestPipeFilter testPipeFilter1 = new TestPipeFilter("Filter1", this);
            TestPipeFilter testPipeFilter2 = new TestPipeFilter("Filter2", this);
            TestPipeFilter testPipeFilter3 = new TestPipeFilter("Filter3", this);

            FilterObserver filterObserver1 = new FilterObserver(this, testPipeFilter1, testPipeFilter2);
            FilterObserver filterObserver2 = new FilterObserver(this, testPipeFilter2, testPipeFilter3);
            FilterObserver filterObserver3 = new FilterObserver(this, testPipeFilter3, null);

            testPipeFilter1.addObserver(filterObserver1);
            testPipeFilter2.addObserver(filterObserver2);
            testPipeFilter3.addObserver(filterObserver3);

            StartFilter = testPipeFilter1;
        }

        public async void execute()
        {
            Console.WriteLine("Start {0}", Name);
            await Task.Run((Action)waitExecute);
        }

        protected void waitExecute()
        {
            try
            {
                Task t = Task.Run((Action)StartFilter.execute);

                if(!t.Wait(1000))
                {
                    Console.WriteLine("Timeout {0}", Name);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Canceld {0}", Name);
            }
        }

        public void notifyEnd()
        {
            Terminate = true;
        }
    }

    interface IObserver
    {
        IPipeFilter InPipeFilter { get; set; }
        IPipeFilter OutPipeFilter { get; set; }
        ILogic Logic { get; set; }
        void notify();
    }

    interface IPipeFilter
    {
        ILogic Logic { get; set; }
        string Name { get; set; }
        List<IObserver> Observers { get; set; }
        void addObserver(IObserver observer);
        void deleteObserver();
        void notifyObserver();
        void execute();
    }

    class FilterObserver : IObserver
    {
        public FilterObserver(ILogic logic, IPipeFilter inPipeFilter, IPipeFilter outPipeFilter)
        {
            Logic = logic;
            InPipeFilter = inPipeFilter;
            OutPipeFilter = outPipeFilter;
        }
        public IPipeFilter InPipeFilter { get; set; }
        public IPipeFilter OutPipeFilter { get; set; }
        public ILogic Logic { get; set; }
        public void notify()
        {
            if (OutPipeFilter != null)
            {
                Task.Run((Action)OutPipeFilter.execute);
            }
            else
            {
                Task.Run((Action)Logic.notifyEnd);
            }
        }
    }

    class TestPipeFilter : IPipeFilter
    {
        public TestPipeFilter()
        {
            Observers = new List<IObserver>();
        }
        public TestPipeFilter(string name)
        {
            Observers = new List<IObserver>();
            Name = name;
        }
        public TestPipeFilter(string name, ILogic logic)
        {
            Observers = new List<IObserver>();
            Name = name;
            Logic = logic;
        }
        public ILogic Logic { get; set; }

        public string Name { get; set; }
        public List<IObserver> Observers { get; set; }

        public void addObserver(IObserver observer)
        {
            Observers.Add(observer);
        }
        public void deleteObserver()
        {
        }
        public void notifyObserver()
        {
            foreach (var observer in Observers)
            {
                observer.notify();
            }
        }
        public void execute()
        {
            Thread.Sleep(5000);
            Console.WriteLine("[{0} - {1}] End.", Logic.Name, Name);
            notifyObserver();
        }

    }
}
