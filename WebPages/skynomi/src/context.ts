import { GenericCache } from "./cache";
import { PluginModule } from "./types";

export class Context {
    public websocket: WebSocket;
    public caches: GenericCache;
    public list_modules: PluginModule[];

    constructor(websocket: WebSocket) {
        this.websocket = websocket;
        this.caches = new GenericCache();
        this.list_modules = [];
    }
}