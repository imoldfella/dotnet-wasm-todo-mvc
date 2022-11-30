using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Features;
//using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;

// ideally we do something like  
// context = Context()
// await do_something(context, )
//  context.cancel()
// but wasm fights us on this...

// WasmEnableThreads
// When we can enable threads, it should be easy! we can do a quick check inside of loops and abandon operations as they are canceled. It might not be worth trying to support without threads. It would take something pretty fancy, like thunks dosome().

// without threads, it's still not too bad; we constantly need to call back to javascript to get parts when we do it gives javascript a chance to tell us to cancel.



// there is no way to cancel a webworker; https://stackoverflow.com/questions/57365381/how-to-cancel-a-wasm-process-from-within-a-webworker
// we need allow multiple workers anyway, but this is not the same since they wouldn't share memory
// for some language runtimes, this potentially can only be single threaded, and not async?
// how does C# manage multiple threads anyway? you can't start workers from a wasm program
// there must be some layer over the top.

namespace TodoMVC
{
    using Factory = Func<string, dynamic[], WasmTask>;

    using D = Dictionary<string, string>;
    using Dfn = Dictionary<string, Func<Parser>>;    

    // WasmTask allows us to use multiple cores, even though only one interfaces to Js.
    public abstract class WasmTask {

       public CancellationTokenSource? tokenSource;

        public int tag;

        public void reply(int status, dynamic? paramsx){
            JsServer.reply(tag, status, paramsx);
        }
        abstract public  Task  run();
    }
    
    public class ExportTask : WasmTask {
        public ExportTask(dynamic[] args) {
            var pr = new PuddleReader();
        }
        public override async Task run() {
             await JsCallback.getPart(tag, "");
            reply(0,null);
        }
    }


    
    public class JsServer
    {
        public static Dictionary<int, WasmTask> active = new Dictionary<int, WasmTask> { };
        public static Dictionary<string, Factory> proc = new Dictionary<string, Factory>{};
        public static Mutex mutex = new Mutex();

        public static void cancel(int tag){
            mutex.WaitOne();
            var t = active[tag];
            t.tokenSource?.Cancel();
            active.Remove(tag);
            mutex.ReleaseMutex();
        }

        public static void exec(int tag, string method, dynamic[] paramsx)
        {
            var factory = proc[method];
            if (factory==null) {
                return;
            }            
            var task = factory(method,paramsx);   
            task.tokenSource = new CancellationTokenSource();
            CancellationToken ct = task.tokenSource.Token;

            // start the task in a thread? How do we cancel it? can any thread call async?
            task.tag = tag;
            active[tag] = task;

            Task t = Task.Factory.StartNew(() =>
            {
                task.run();
            });
        }

        public static void reply(int tag, int status, dynamic? paramsx)
        {
            // we need some kind of mutex or queue here before deleting
            active.Remove(tag);

        }

        // wasm tasks work by reading and writing a pipeline to javascript
        public static async Task<OoxmlPart> getPart(int readTag, string path)
        {
            return new OoxmlPart();
        }
        public static async Task callbackWrite(int tag, byte[] data)
        {

        }    
    }
    public class OoxmlServer : JsServer{
        static int init(){
            proc["export"] = delegate(string method, dynamic[] args) { return new ExportTask(args); };
            proc["import"] = delegate(string method, dynamic[] args) { return new ExportTask(args); };            
            return 0;
        }
    }
    // are untyped arguments a possible lateral movement vector? is there a better way?
    // does typing them make it harder? type inside or outside or both?



    public class JsCallback
    {


    }



    public class Parser
    {
        PuddleReader reader;
        Parser(PuddleReader r)
        {
            this.reader = r;
        }
    }
    // probably an interface with different implementations for cli/test and production
    public class PuddleWriter
    {
        MainDocumentPart part;
        async Task<byte[]> readPart(String path)
        {
            return new byte[] { };
        }
        async Task writeCbor(byte[] data)
        {

        }
    }
    public class Element
    {
        public string name;
        Dictionary<string, string> attribute = new Dictionary<string, string> { };
    }
    public class PuddleReader
    {
        Dfn parser = new Dfn();

        int tag = 0;


        void startElement(Element e)
        {
            var f = parser[e.name];
            if (f != null)
            {
                f();
            }
        }
        void close()
        {
        }
    }


    public class OoxmlPart
    {
        int compression;
        byte[] data = new byte[] { };
    }



    // primarily this is an html like dom, but we need to manage the styles.
    public abstract class PuddleVisitor
    {
        D parser = new D();
        public abstract void startWordPart(string path);
        public abstract void startWorkbookPart();
        public abstract void close();

        async Task writePart(byte[] data)
        {
        }
    }

    public partial class Controller
    {
        private string? _activeRoute;
        private string? _lastActiveRoute;
        private Store store { get; }
        private View view { get; }

        public Controller(Store store, View view)
        {
            this.store = store;
            this.view = view;

            view.BindAddItem(AddItem);
            view.BindEditItemSave(EditItemSave);
            view.BindEditItemCancel(EditItemCancel);
            view.BindRemoveItem(RemoveItem);
            view.BindToggleItem((id, completed) =>
            {
                ToggleCompleted(id, completed);
                _filter(true);
            });
            view.BindRemoveCompleted(RemoveCompletedItems);
            view.BindToggleAll(ToggleAll);

            _activeRoute = "";
            _lastActiveRoute = null;
        }

        [GeneratedRegex("^#\\/")]
        private static partial Regex GetUrlHashRegex();

        public void SetView(string? urlHash)
        {
            var route = GetUrlHashRegex().Replace(urlHash ?? "", "");
            _activeRoute = route;
            _filter();
            view.UpdateFilterButtons(route);
        }

        public void AddItem(string title)
        {
            store.Insert(new Item
            {
                Id = DateTime.UtcNow.Ticks / 10000,
                Title = title,
                Completed = false

            });

            view.ClearNewTodo();
            _filter(true);
        }

        public void EditItemSave(long id, string title)
        {
            if (title.Length != 0)
            {
                store.Update(new Item { Id = id, Title = title });
                view.EditItemDone(id, title);
            }
            else
            {
                RemoveItem(id);
            }
        }

        public void EditItemCancel(long id)
        {
            var items = store.Find(id, null, null);
            var title = items[0].Title!;
            view.EditItemDone(id, title);
        }

        public void RemoveItem(long id)
        {
            store.Remove(id, null, null);
            _filter();
            view.RemoveItem(id);
        }

        public void RemoveCompletedItems()
        {
            store.Remove(null, null, true);
            _filter(true);
        }

        public void ToggleCompleted(long id, bool completed)
        {
            store.Update(new Item { Id = id, Completed = completed });
            view.SetItemComplete(id, completed);
        }

        public void ToggleAll(bool completed)
        {
            var todos = store.Find(null, null, !completed);
            foreach (var item in todos)
            {
                ToggleCompleted(item.Id, completed);
            }
            _filter(true);
        }


        void _filter(bool force = false)
        {
            var route = _activeRoute;

            if (force || _lastActiveRoute != "" || _lastActiveRoute != route)
            {
                var todos = route switch
                {
                    "active" => store.Find(null, null, false),
                    "completed" => store.Find(null, null, true),
                    _ => store.Find(null, null, null),
                };
                view.ShowItems(todos);
            }

            var count = store.Count();
            view.SetItemsLeft(count.active);
            view.SetClearCompletedButtonVisibility(count.completed != 0);
            view.SetCompleteAllCheckbox(count.completed == count.total);
            view.SetMainVisibility(count.total != 0);
            _lastActiveRoute = route;
        }
    }
}