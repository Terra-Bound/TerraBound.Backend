import adapter from '@sveltejs/adapter-static';
import path from "path";

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			pages: path.resolve(new URL(import.meta.url).pathname, '../dist'), // Hier wird die Seite gebaut
			assets: path.resolve(new URL(import.meta.url).pathname, '../dist'), // Assets werden hier abgelegt
			fallback: 'index.html',    // Aktiviert den Fallback-Modus 
		}),
		paths: {
			base: '', // Falls du eine bestimmte Basis-URL hast, passe dies an
		},
	},
};

export default config;
