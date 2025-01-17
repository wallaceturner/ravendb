import React from "react";
import { Meta, StoryObj } from "@storybook/react";
import { withStorybookContexts, withBootstrap5 } from "test/storybookTestUtils";
import GatherDebugInfo from "./GatherDebugInfo";
import { mockStore } from "test/mocks/store/MockStore";
import { DatabasesStubs } from "test/stubs/DatabasesStubs";
import { ShardedDatabaseSharedInfo } from "components/models/databases";

export default {
    title: "Pages/ManageServer",
    component: GatherDebugInfo,
    decorators: [withStorybookContexts, withBootstrap5],
} satisfies Meta<typeof GatherDebugInfo>;

export const DefaultGatherDebugInfo: StoryObj<typeof GatherDebugInfo> = {
    name: "Gather Debug Info",
    render: () => {
        const clusterDb = DatabasesStubs.nonShardedClusterDatabase().toDto();
        const shardedDb = DatabasesStubs.shardedDatabase().toDto() as ShardedDatabaseSharedInfo;

        mockStore.databases.withDatabases([clusterDb, shardedDb]);

        return <GatherDebugInfo />;
    },
};
