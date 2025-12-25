import type { PluginModule } from "./types";
import * as core from "./skynomi";
import { Context } from "./context";
import { load_module } from "./loader";

let context: Context = null!;
let container: HTMLElement = null!;

document.addEventListener("DOMContentLoaded", () => {
    console.log("Loading....")
    const ws_url = document.getElementById("app")!.getAttribute("ws-url")!;
    const websocket = new WebSocket(ws_url);

    context = new Context(websocket);
    container = document.getElementById("app")!;

    context.websocket.addEventListener("open", e => {
        console.log("Connected to server");
    })

    context.websocket.addEventListener("message", e => {
        interface Data {
            eventType: string,
            data: {
                Modules: PluginModule[],
                Player: {}
            }
        }

        const data: Data = JSON.parse(e.data);
        if (data.eventType.split(":")[0] != "core") return;


        context.list_modules = data.data.Modules;

        context.caches.set("core:player", data.data.Player);
    })

    context.websocket.addEventListener("close", e => {
        console.log("Disconnected from server");
    })

    console.log("Loading module: core...");
    load_module("core", container, context);
    console.log("Loaded module: core");
});

document.querySelectorAll("a[data-spa]").forEach(link => {
    link.addEventListener("click", e => {
        e.preventDefault();
        const target = e.target as HTMLAnchorElement;
        const path = target.getAttribute("href")?.slice(1)!;

        load_module(path, container, context);
        history.pushState({}, "", target!.href);
    });
});

window.addEventListener("popstate", () => {
    let path = location.pathname.slice(1);
    if (path === "") path = "core";

    load_module(path, container, context);
});