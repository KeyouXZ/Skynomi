// src/cache.ts
class GenericCache {
  cache = new Map;
  set(key, value) {
    this.cache.set(key, value);
  }
  get(key) {
    return this.cache.get(key);
  }
  getAllKeys() {
    return Array.from(this.cache.keys());
  }
  has(key) {
    return this.cache.has(key);
  }
  delete(key) {
    return this.cache.delete(key);
  }
  clear() {
    this.cache.clear();
  }
}

// src/context.ts
class Context {
  websocket;
  caches;
  list_modules;
  constructor(websocket) {
    this.websocket = websocket;
    this.caches = new GenericCache;
    this.list_modules = [];
  }
}

// src/skynomi.ts
var css = `
body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
            color: #333;
        }
        header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 60px 20px;
            text-align: center;
        }
        header h1 {
            margin: 0;
            font-size: 3em;
        }
        header p {
            font-size: 1.2em;
            margin: 20px 0;
        }
        .cta-button {
            background-color: #ff6b6b;
            color: white;
            padding: 15px 30px;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            transition: background-color 0.3s;
            display: inline-block;
        }
        .cta-button:hover {
            background-color: #ff5252;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 40px 20px;
        }
        .features {
            text-align: center;
            margin-bottom: 60px;
        }
        .features h2 {
            font-size: 2.5em;
            margin-bottom: 20px;
        }
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
        }
        .card {
            background: white;
            border-radius: 8px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
            padding: 20px;
            text-align: center;
            transition: transform 0.3s;
        }
        .card:hover {
            transform: translateY(-5px);
        }
        .card h3 {
            margin-top: 0;
            color: #667eea;
        }
        .card p {
            margin: 15px 0;
        }
        footer {
            background-color: #333;
            color: white;
            text-align: center;
            padding: 20px;
        }`;
var core_module = {
  name: "core",
  init: (container, ctx) => {
    const style = document.createElement("style");
    style.textContent = css;
    document.head.appendChild(style);
    createLandingPage(container);
  },
  unload: (container, ctx) => {}
};
var features = [
  {
    title: "Custom Currencies",
    description: "Create and manage multiple currencies tailored to your server's needs, from gold coins to rare gems."
  },
  {
    title: "Shop System",
    description: "Set up player-run shops with ease. Buy, sell, and trade items securely within the game."
  },
  {
    title: "Banking Features",
    description: "Players can deposit, withdraw, and transfer funds with built-in security measures."
  },
  {
    title: "Admin Controls",
    description: "Full administrative tools for monitoring transactions, adjusting rates, and preventing exploits."
  },
  {
    title: "Integration Ready",
    description: "Seamlessly integrates with other TShock plugins for a cohesive server experience."
  },
  {
    title: "Real-Time Updates",
    description: "Live economy tracking and notifications keep players engaged and informed."
  }
];
function createLandingPage(app) {
  const header = document.createElement("header");
  header.innerHTML = `
                <h1>Skynomi</h1>
                <p>Modern Economy System for TShock Plugin</p>
                <p>Enhance your Terraria server with a robust, user-friendly economy that integrates seamlessly with TShock. Manage currencies, shops, and player interactions effortlessly.</p>
                <a href="#" class="cta-button">Download Now</a>
            `;
  app.appendChild(header);
  const container = document.createElement("div");
  container.className = "container";
  const featuresSection = document.createElement("section");
  featuresSection.className = "features";
  featuresSection.innerHTML = `
                <h2>Key Features</h2>
                <p>Discover what makes Skynomi the go-to economy plugin for Terraria servers.</p>
            `;
  container.appendChild(featuresSection);
  const grid = document.createElement("div");
  grid.className = "grid";
  features.forEach((feature) => {
    const card = document.createElement("div");
    card.className = "card";
    card.innerHTML = `
                    <h3>${feature.title}</h3>
                    <p>${feature.description}</p>
                `;
    grid.appendChild(card);
  });
  container.appendChild(grid);
  app.appendChild(container);
  const footer = document.createElement("footer");
  footer.innerHTML = `
                <p>&copy; 2025 Skynomi. All rights reserved. | <a href="#" style="color: #ff6b6b;">Contact Us</a></p>
            `;
  app.appendChild(footer);
}

// src/loader.ts
var current_module = core_module;
function call_not_found(container, module_name) {
  current_module = null;
  container.innerHTML = `<h2>404 - Page Not Found</h2>
        <p>The page "${module_name}" does not exist.</p>`;
}
async function load_module(module_name, container, ctx) {
  if (module_name == "core") {
    current_module = core_module;
    core_module.init(container, ctx);
    return;
  }
  if (!ctx.list_modules.find((x) => x.Name.toLowerCase().replaceAll(" ", "") == module_name)) {
    return call_not_found(container, module_name);
  }
  if (current_module?.unload)
    current_module.unload(container, ctx);
  try {
    console.log("Loading module: " + module_name + "...");
    const module = await import(`./${module_name}.js`);
    current_module = module;
    module.init(container, ctx);
    console.log("Loaded module: " + module_name);
  } catch (Er) {
    call_not_found(container, module_name);
    console.error(Er);
  }
}

// src/core.ts
var context = null;
var container = null;
document.addEventListener("DOMContentLoaded", () => {
  console.log("Loading....");
  const ws_url = document.getElementById("app").getAttribute("ws-url");
  const websocket = new WebSocket(ws_url);
  context = new Context(websocket);
  container = document.getElementById("app");
  context.websocket.addEventListener("open", (e) => {
    console.log("Connected to server");
  });
  context.websocket.addEventListener("message", (e) => {
    const data = JSON.parse(e.data);
    if (data.eventType.split(":")[0] != "core")
      return;
    context.list_modules = data.data.Modules;
    context.caches.set("core:player", data.data.Player);
  });
  context.websocket.addEventListener("close", (e) => {
    console.log("Disconnected from server");
  });
  console.log("Loading module: core...");
  load_module("core", container, context);
  console.log("Loaded module: core");
});
document.querySelectorAll("a[data-spa]").forEach((link) => {
  link.addEventListener("click", (e) => {
    e.preventDefault();
    const target = e.target;
    const path = target.getAttribute("href")?.slice(1);
    load_module(path, container, context);
    history.pushState({}, "", target.href);
  });
});
window.addEventListener("popstate", () => {
  let path = location.pathname.slice(1);
  if (path === "")
    path = "core";
  load_module(path, container, context);
});
