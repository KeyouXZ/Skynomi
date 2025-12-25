import { Context } from "./context";
import { Module } from "./types";

const css = `
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
        }`
        
export let core_module: Module = {
    name: "core",
    init: (container: HTMLElement, ctx: Context) => {
        const style = document.createElement('style');
        style.textContent = css;
        document.head.appendChild(style);
        createLandingPage(container);
    },
    unload: (container: HTMLElement, ctx: Context) => { }
}

const features = [
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

// Function to create the landing page content
function createLandingPage(app: HTMLElement) {
    // Header
    const header = document.createElement('header');
    header.innerHTML = `
                <h1>Skynomi</h1>
                <p>Modern Economy System for TShock Plugin</p>
                <p>Enhance your Terraria server with a robust, user-friendly economy that integrates seamlessly with TShock. Manage currencies, shops, and player interactions effortlessly.</p>
                <a href="#" class="cta-button">Download Now</a>
            `;
    app.appendChild(header);

    // Container
    const container = document.createElement('div');
    container.className = 'container';

    // Features section
    const featuresSection = document.createElement('section');
    featuresSection.className = 'features';
    featuresSection.innerHTML = `
                <h2>Key Features</h2>
                <p>Discover what makes Skynomi the go-to economy plugin for Terraria servers.</p>
            `;
    container.appendChild(featuresSection);

    // Grid
    const grid = document.createElement('div');
    grid.className = 'grid';

    // Generate cards from array
    features.forEach(feature => {
        const card = document.createElement('div');
        card.className = 'card';
        card.innerHTML = `
                    <h3>${feature.title}</h3>
                    <p>${feature.description}</p>
                `;
        grid.appendChild(card);
    });

    container.appendChild(grid);
    app.appendChild(container);

    // Footer
    const footer = document.createElement('footer');
    footer.innerHTML = `
                <p>&copy; 2025 Skynomi. All rights reserved. | <a href="#" style="color: #ff6b6b;">Contact Us</a></p>
            `;
    app.appendChild(footer);
}
