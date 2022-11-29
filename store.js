export function setLocalStorage(todosJson) {
	window.localStorage.setItem('dotnet-wasm-todomvc', todosJson);
	return new Promise((resolve, reject) => {
		resolve(1)
	})
}

export function getLocalStorage() {
	return window.localStorage.getItem('dotnet-wasm-todomvc') || '[]';
};
