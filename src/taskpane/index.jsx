import * as React from "react";
import { createRoot } from "react-dom/client";
import App from "./components/App";
import { FluentProvider, webLightTheme } from "@fluentui/react-components";

/* global document, Office, module, require */

const title = "Parafrase Gandi";
let root;

/* Render application after Office initializes */
Office.onReady(() => {
  const rootElement = document.getElementById("container");
  root = createRoot(rootElement);
  
  root.render(
    <FluentProvider theme={webLightTheme}>
      <App title={title} />
    </FluentProvider>
  );
});

if (module.hot) {
  module.hot.accept("./components/App", () => {
    const NextApp = require("./components/App").default;
    root?.render(
      <FluentProvider theme={webLightTheme}>
        <NextApp title={title} />
      </FluentProvider>
    );
  });
}

