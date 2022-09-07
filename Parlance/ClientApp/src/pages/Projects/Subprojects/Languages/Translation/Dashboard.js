import {useTranslation} from "react-i18next";
import {useNavigate, useParams} from "react-router-dom";
import React, {useEffect, useState} from "react";
import Fetch from "../../../../../helpers/Fetch";
import i18n from "../../../../../helpers/i18n";
import Overview from "./Overview";
import ListPage from "../../../../../components/ListPage";
import Hero from "../../../../../components/Hero";

export default function Dashboard(props) {
    const {project, subproject, language} = useParams();
    const [data, setData] = useState();
    const navigate = useNavigate();
    const {t} = useTranslation();
    
    const updateData = async () => {
        setData(await Fetch.get(`/api/projects/${project}/${subproject}/${language}`));
    }
    
    useEffect(() => {
        updateData();
    }, []);
    
    //TODO
    if (!data) return <div>
        Please wait
    </div>
    
    const items = [
        t("Dashboard"),
        {
            name: t("Overview"),
            render: <Overview data={data}/>
        }
    ];
    
    return <div style={{display: "flex", flexDirection: "column", flexGrow: 1}}>
        <Hero heading={i18n.humanReadableLocale(language)} subheading={data.subprojectName} buttons={[
            {
                text: t("Translate"),
                onClick: () => navigate("translate")
            }
        ]} />
        <ListPage items={items} />
    </div>
}