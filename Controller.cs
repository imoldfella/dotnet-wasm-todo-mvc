﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TodoMVC
{
    // probably an interface with different implementations for cli/test and production
    public class PuddleWriter {
        async Task<byte[]> readPart(String path) {
            return  new byte[]{};
        }
        async Task writeCbor(byte[] data) {

        }
    }
    public class Element {
    }
    public interface Parser {
        PuddleVisitor visitor;
        startElement(Element e){
        }
        close(){
        }
    }
    // primarily this is an html like dom, but we need to manage the styles.
    public interface PuddleVisitor {
        Map<String, Parser> parser ={};
        void startWordPart(string path);
        void startWorkbookPart( );
        void close();
        async Task writePart(byte[] data){
        }
    }
    public class PuddleReader {
        
        static 
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