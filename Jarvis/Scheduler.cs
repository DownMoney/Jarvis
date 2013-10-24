using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Threading;

namespace Jarvis
{
    public class Scheduler
    {
        private struct Task
        {
            public string input;
            public string ruleName;
            public Timer timer;
            public int due;
            public string type;
            public DateTime datetime;
            public bool repeat;
            public int ID;
            public bool Keep;
        }
        private int lastID = 0;
        private Task[] Tasks = new Task[0];
        public event MessageReceived OnTick;

        public Scheduler()
        {
            LoadSchedule();
        }

        public void AddTask(string input, string ruleName, int due, bool repeat, bool keep=false)
        {
            Task t = new Task();
            t.input = input;
            t.ruleName = ruleName;
            t.due = due;
            t.repeat = repeat;
            t.type = "periodic";
            t.Keep = keep;

            if(t.due>0)
                t.timer = new Timer(new TimerCallback(Tick), t, t.due, Timeout.Infinite);

            Array.Resize<Task>(ref Tasks, Tasks.Length + 1);
            Tasks[Tasks.Length - 1] = t;

            WriteToSchedule(t);
        }

        public void AddTask(string input, string ruleName, string due, bool repeat, bool keep=false)
        {
            Task t = new Task();
            t.input = input;
            t.ruleName = ruleName;
            DateTime dt = DateTime.Parse(due);
            TimeSpan ts = dt.Subtract(DateTime.Now);
            t.due = Convert.ToInt32(ts.TotalMilliseconds);
            t.datetime = dt;
            t.repeat = repeat;
            t.type = "due";
            t.Keep = keep;

            if(t.due>0)
                t.timer = new Timer(new TimerCallback(Tick), t, t.due, Timeout.Infinite);
            
            Array.Resize<Task>(ref Tasks, Tasks.Length + 1);
            Tasks[Tasks.Length - 1] = t;

            WriteToSchedule(t);
        }

        private void WriteToSchedule(Task task)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Settings/Schedule.xml");

            XmlElement prov = (XmlElement)(doc.DocumentElement.AppendChild(doc.CreateNode(XmlNodeType.Element, "Task", "")));

            lastID++;
            task.ID = lastID;
            doc.DocumentElement.Attributes["lastId"].InnerText = lastID.ToString();
            prov.SetAttribute("ID", task.ID.ToString());
            prov.SetAttribute("keep", task.Keep.ToString());
            prov.SetAttribute("type", task.type);
            if(task.type=="periodic")
            prov.SetAttribute("val", task.due.ToString());
            else
                prov.SetAttribute("val", task.datetime.ToShortTimeString());

            prov.SetAttribute("repeat", task.repeat.ToString());

            prov.InnerXml = "<Input>"+task.input+"</Input><RuleName>"+task.ruleName+"</RuleName>";

            doc.Save("Settings/Schedule.xml");
        }

        private void LoadSchedule()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Settings/Schedule.xml");

            lastID = Convert.ToInt32(doc.DocumentElement.Attributes["lastId"].InnerText);

            XmlNodeList tasks = doc.SelectNodes("/Schedule/Task");

            for (int i = 0; i < tasks.Count; i++)
            {
                Array.Resize<Task>(ref Tasks, Tasks.Length + 1);

                if (tasks[i].Attributes["type"].InnerText.ToLower() == "period")
                    Tasks[Tasks.Length-1] = PeriodicTask(tasks[i]);

                if (tasks[i].Attributes["type"].InnerText.ToLower() == "due")
                    Tasks[Tasks.Length - 1] = DueTask(tasks[i]);
            }
        }

        private Task PeriodicTask(XmlNode task)
        {
            Task t = new Task();

            t.input = task.ChildNodes[0].InnerText;
            t.ruleName = task.ChildNodes[1].InnerText;
            t.ID = Convert.ToInt32(task.Attributes["ID"].InnerText);
            t.due = Convert.ToInt32(task.Attributes["val"].InnerText);
            t.repeat = Convert.ToBoolean(task.Attributes["repeat"].InnerText);
            t.Keep = Convert.ToBoolean(task.Attributes["keep"].InnerText);

            if(t.due>0)
                t.timer = new Timer(new TimerCallback(Tick),t,t.due,Timeout.Infinite );
           
            return t;
        }

        private Task DueTask(XmlNode task)
        {
            Task t = new Task();

            t.input = task.ChildNodes[0].InnerText;
            t.ruleName = task.ChildNodes[1].InnerText;
            t.ID = Convert.ToInt32(task.Attributes["ID"].InnerText);
            string val = task.Attributes["val"].InnerText;
            DateTime due = DateTime.Parse(val);
            TimeSpan span = due.Subtract(DateTime.Now);
            t.due = Convert.ToInt32(span.TotalMilliseconds);
            t.repeat = Convert.ToBoolean(task.Attributes["repeat"].InnerText);
            t.Keep = Convert.ToBoolean(task.Attributes["keep"].InnerText);

            if(t.due>0)
                t.timer = new Timer(new TimerCallback(Tick), t, t.due, Timeout.Infinite);

            return t;
        }

        public void RemoveTask(int ID)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Settings/Schedule.xml");
            XmlNode node = doc.SelectSingleNode("/Schedule/Task[@ID=\"" + ID.ToString() + "\"]");
            doc.DocumentElement.RemoveChild(node);
        }
         
        private void RemoveTask(Task task)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Settings/Schedule.xml");
            XmlNode node = doc.SelectSingleNode("/Schedule/Task[@ID=\"" + task.ID.ToString() + "\"]");
            try
            {
                doc.DocumentElement.RemoveChild(node);
            }
            catch
            {

            }
        }

        private void Tick(object o)
        {
            Task t = (Task)o;

            if (!t.Keep)
                RemoveTask(t);

            if (t.repeat)
            {
                if (t.type == "due")                   
                {
                    TimeSpan span = t.datetime.Subtract(DateTime.Now);
                    t.due = Convert.ToInt32(span.TotalMilliseconds); 
                }

                t.timer = new Timer(new TimerCallback(Tick), t, t.due, Timeout.Infinite);
            }

            tick(t.input, t.ruleName);
        }

        protected virtual void tick(string input, string ruleName)
        {
            OnTick(input, ruleName);
        }
    }
}
