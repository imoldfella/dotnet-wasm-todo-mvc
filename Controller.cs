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

namespace TodoMVC
{
    using D = Dictionary<string, string>;
    using Dfn = Dictionary<string, Func<Parser>>;

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

    public class JsServer
    {
        static Dictionary<int, PuddleReader> active = new Dictionary<int, PuddleReader> { };
        static async Task export(int tag)
        {
            var pr = new PuddleReader();

            await JsCallback.getPart(tag, "");
            JsCallback.status(tag, 1);
        }
        static void import(int tag)
        {

        }

    }
    public class JsCallback
    {
        public static void status(int tag, int status)
        {

        }
        public static async Task<OoxmlPart> getPart(int readTag, string path)
        {
            return new OoxmlPart();
        }
        public static async Task callbackWrite(int tag, byte[] data)
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