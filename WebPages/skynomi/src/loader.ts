import { Context } from "./context";
import { Module } from "./types";
import * as core from "./skynomi";

let current_module: Module | null = core.core_module;

function call_not_found(container: HTMLElement, module_name: string) {
    current_module = null;
    container.innerHTML = `<h2>404 - Page Not Found</h2>
        <p>The page "${module_name}" does not exist.</p>`;
}

export async function load_module(module_name: string, container: HTMLElement, ctx: Context) {
    if (module_name == "core") {
        current_module = core.core_module;
        core.core_module.init(container, ctx);
        return;
    }
    
    if (!ctx.list_modules.find(x =>
        x.Name.toLowerCase().replaceAll(" ", "") == module_name
    )) {
        return call_not_found(container, module_name);
    }

    if (current_module?.unload) current_module.unload(container, ctx);


    try {
        console.log("Loading module: " + module_name + "...");
        const module: Module = await import(`./${module_name}.js`);

        current_module = module;
        module.init(container, ctx);

        console.log("Loaded module: " + module_name);
    } catch (Er) {
        call_not_found(container, module_name);
        console.error(Er);
    }
}