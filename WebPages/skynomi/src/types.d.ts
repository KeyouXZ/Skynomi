import { Context } from "./context";

export interface Module {
    name: string;
    init: (container: HTMLElement, ctx: Context) => void;
    unload: (container: HTMLElement, ctx: Context) => void;
}

export interface PluginModule {
    Name: string;
    Description: string;
    Author: string;
    Version: string;
}