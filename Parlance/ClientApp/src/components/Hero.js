import Styles from "./Hero.module.css"
import Container from "./Container";
import PageHeading from "./PageHeading";
import React from "react";
import Button from "./Button";
import SmallButton from "./SmallButton";

export default function Hero({heading, subheading, buttons}) {
    return  <Container bottomBorder={true} className={Styles.heroContainer}>
        <div className={Styles.heroInner}>
            <PageHeading>{heading}</PageHeading>
            <PageHeading level={2}>{subheading}</PageHeading>
            <div className={Styles.buttonBox}>
                {buttons.map(button => <SmallButton onClick={button.onClick}>{button.text}</SmallButton>)}
            </div>
        </div>
    </Container>
}