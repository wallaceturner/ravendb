﻿import React from "react";
import { Alert, Button, Modal, ModalBody, ModalFooter } from "reactstrap";
import { Icon } from "components/common/Icon";
import { useAppSelector } from "components/store";
import { documentRevisionsSelectors } from "./store/documentRevisionsSliceSelectors";

interface EnforceConfigurationProps {
    toggle: () => void;
    onConfirm: () => Promise<void>;
}

export default function EnforceConfiguration(props: EnforceConfigurationProps) {
    const { toggle, onConfirm } = props;

    const collectionConfigsNames = useAppSelector(documentRevisionsSelectors.collectionConfigs).map(
        (config) => config.Name
    );

    const onSubmit = () => {
        onConfirm();
        toggle();
    };

    return (
        <Modal isOpen toggle={toggle} wrapClassName="bs5" contentClassName="modal-border bulge-warning">
            <ModalBody className="vstack gap-2">
                {collectionConfigsNames.length > 0 && (
                    <>
                        <h4>Enforce Revision Configuration</h4>
                        <p>The following collections have a revision configuration defined:</p>
                        <ul>
                            {collectionConfigsNames.map((name) => (
                                <li key={name}>{name}</li>
                            ))}
                        </ul>
                    </>
                )}
                <p>
                    Clicking <strong>Enforce</strong> will enforce the current revision configuration definitions{" "}
                    <strong>on all existing revisions</strong> in the database per collection.
                </p>
                <p>Revisions might be removed depending on the current configuration rules.</p>
                <Alert color="warning">
                    <p>For collections without a specific revision configuration:</p>
                    <ul>
                        <li>
                            <strong>Non-conflicting documents</strong>
                            <br />
                            If Document Defaults are defined & enabled, it will be applied. If not defined, or if
                            disabled, <strong>all non-conflicting document revisions will be deleted.</strong>
                        </li>
                        <li className="mt-3">
                            <strong>Conflicting documents</strong>
                            <br />
                            If Conflicting Document Defaults are enabled, it will be applied to conflicting document
                            revisions. If disabled, <strong>all conflicting document revisions will be deleted.</strong>
                        </li>
                    </ul>
                </Alert>
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={toggle}>
                    Cancel
                </Button>
                <Button color="warning" onClick={onSubmit}>
                    <Icon icon="rocket" />
                    Enforce Configuration
                </Button>
            </ModalFooter>
        </Modal>
    );
}
