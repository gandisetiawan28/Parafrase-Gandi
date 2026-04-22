import { useState } from "react";
import { TabList, Tab } from "@fluentui/react-components";
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
    backgroundColor: "#F3F2F1",
    display: "flex",
    flexDirection: "column",
  },
  nav: {
    backgroundColor: "#FFFFFF",
    borderBottom: "1px solid #E1E1E1",
    position: "sticky",
    top: 0,
    zIndex: 100,
  }
});

const App = (props) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState("paraphrase");

  const onTabSelect = (event, data) => {
    setSelectedTab(data.value);
  };

  return (
    <div className={styles.root}>
      <div className={styles.nav}>
        <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
          <Tab value="paraphrase" icon={<DocumentCopy24Regular />}>Parafrase</Tab>
          <Tab value="upload" icon={<DocumentArrowUp24Regular />}>Upload</Tab>
          <Tab value="citation" icon={<Search24Regular />}>Sitasi</Tab>
        </TabList>
      </div>
      
      <div style={{ flex: 1, padding: "10px" }}>
        {selectedTab === "paraphrase" && <GeminiConfig />}
        {selectedTab === "upload" && <DocumentPage />}
        {selectedTab === "citation" && <CitationPage />}
      </div>
    </div>
  );
};

App.propTypes = {
  title: PropTypes.string,
};

export default App;
