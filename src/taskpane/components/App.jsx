import * as React from "react";
import { useState } from "react";
import PropTypes from "prop-types";
import { makeStyles, shorthands, TabList, Tab, Button, Spinner } from "@fluentui/react-components";
import { 
  DocumentCopy24Regular, 
  DocumentArrowUp24Regular, 
  Search24Regular 
} from "@fluentui/react-icons";
import GeminiConfig from "./GeminiConfig";
import DocumentPage from "./DocumentPage";
import CitationPage from "./CitationPage";

const useStyles = makeStyles({
  root: {
    minHeight: "100vh",
    backgroundColor: "#F8F9FA",
    display: "flex",
    flexDirection: "column",
    fontFamily: "'Inter', sans-serif",
  },
  nav: {
    backgroundColor: "rgba(255, 255, 255, 0.8)",
    backdropFilter: "blur(10px)",
    borderBottom: "1px solid rgba(225, 225, 225, 0.5)",
    position: "sticky",
    top: 0,
    zIndex: 100,
    ...shorthands.padding("5px", "10px"),
    boxShadow: "0 2px 10px rgba(0,0,0,0.03)",
  },
  tabList: {
    justifyContent: "space-around",
  }
});

const App = (props) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState("paraphrase");
  const [hasError, setHasError] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const onTabSelect = (event, data) => {
    setSelectedTab(data.value);
  };

  if (hasError) {
    return (
      <div style={{ padding: "20px", color: "red" }}>
        <h3>Terjadi Kesalahan UI</h3>
        <p>{errorMessage}</p>
        <Button onClick={() => window.location.reload()}>Muat Ulang</Button>
      </div>
    );
  }

  try {
    return (
      <div className={styles.root}>
        <div className={styles.nav}>
          <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
            <Tab value="paraphrase" icon={DocumentCopy24Regular ? <DocumentCopy24Regular /> : null}>Parafrase</Tab>
            <Tab value="upload" icon={DocumentArrowUp24Regular ? <DocumentArrowUp24Regular /> : null}>Upload</Tab>
            <Tab value="citation" icon={Search24Regular ? <Search24Regular /> : null}>Sitasi</Tab>
          </TabList>
        </div>
        
        <div style={{ flex: 1, padding: "10px" }}>
          <React.Suspense fallback={<Spinner />}>
            {selectedTab === "paraphrase" && (GeminiConfig ? <GeminiConfig /> : <div>Memuat Parafrase...</div>)}
            {selectedTab === "upload" && (DocumentPage ? <DocumentPage /> : <div>Memuat Upload...</div>)}
            {selectedTab === "citation" && (CitationPage ? <CitationPage /> : <div>Memuat Sitasi...</div>)}
          </React.Suspense>
        </div>
      </div>
    );
  } catch (e) {
    setHasError(true);
    setErrorMessage(e.message);
    return null;
  }
};

App.propTypes = {
  title: PropTypes.string,
};

export default App;
