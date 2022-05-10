#!/usr/bin/python3

from multibench import *
import multibench

# Insturction:
# 1. Make a copy of this file and rename to run_multi_bench.py
# 2. Change SERVER_LIST to remote servers with RAC server
# 3. see jsons below to define a workload

SERVER_LIST = ["192.168.41.237", 
                "192.168.41.100",
                "192.168.41.76",
                "192.168.41.161",
                "192.168.41.248",
                "192.168.41.31",
                "192.168.41.254",
                "192.168.41.85",
                "192.168.41.86",
                "192.168.41.79" ]


if __name__ == "__main__":
    test = {
        "nodes_pre_server": 1,
        "use_server": 5,
        "client_multiplier": [10],

        "typecode": "rc",
        "total_objects": [100],

        "prep_ops_pre_obj": 1000,
        "num_reverse": 100,
        "prep_ratio": [1, 0, 0],


        "ops_per_object": 1000,
        "op_ratio": [0.25, 0.25, 0.5],
        "target_throughput": 0
    }
    #run_experiment(test, "client_multiplier", "total_objects", "test_10_client", SERVER_LIST, True)
    test = {
        "nodes_pre_server": 1,
        "use_server": 5,
        "client_multiplier": [200],

        "typecode": "rc",
        "total_objects": [100],

        "prep_ops_pre_obj": 1000,
        "num_reverse": 100,
        "prep_ratio": [1, 0, 0],


        "ops_per_object": 1000,
        "op_ratio": [0.25, 0.25, 0.5],
        "target_throughput": 0
    }

    run_experiment(test, "client_multiplier", "total_objects", "test_1_client", SERVER_LIST, True)
    test = {
        "nodes_pre_server": 1,
        "use_server": 5,
        "client_multiplier": [100],

        "typecode": "rc",
        "total_objects": [100],

        "prep_ops_pre_obj": 1,
        "num_reverse": 100,
        "prep_ratio": [1, 0, 0],


        "ops_per_object": 900,
        "op_ratio": [0.5, 0.5, 0],
        "target_throughput": 0
    }
    multibench.run_name = "sweep"
    i = 1;
    while i < 10:
        print("Running client_multiplier " + str(i))
        test["client_multiplier"] = [i]
        run_experiment(test, "client_multiplier", "total_objects", "test_1_client_insert_" + str(i), SERVER_LIST, True)
        i += 1
