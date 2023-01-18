import ServerLanguageListing from "./ServerLanguageListing";
import {Route, Routes} from "react-router-dom";
import ServerLanguageProjectListing from "./ServerLanguageProjectListing";


export default function(props) {
    return <Routes>
        <Route element={<ServerLanguageListing />} path={"/"} />
        <Route element={<ServerLanguageProjectListing />} path={"/:language"} />
    </Routes>
}