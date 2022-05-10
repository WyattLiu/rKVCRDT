import matplotlib.pyplot as plt
import numpy as np
import time
import sys
import re
from collections import defaultdict
import statistics

class latency_analyzer:
    def debug_print(self, str):
        print("INFO: latency_analyzer: " + str)
    def __init__(self, filename_):
        self.filename = filename_
        self.buffer = []
        self.ops = defaultdict(list)
        self.res = []
        self.all_lt = []
        self.debug_print("Construct: " + filename_)
        self.read_into_buffer()
        self.parse_buffer()
        res = self.analysis()
        
    def read_into_buffer(self):
        with open(self.filename) as f:
            self.buffer = f.readlines()
    def parse_buffer(self):
        for line in self.buffer:
            p = re.compile('\(\'(.*)\', (.*), (.*)\)')
            if(p.match(line)):
                match = p.search(line)
                self.ops[match.groups()[0]].append((float(match.groups()[1]), int(match.groups()[2])))
        print("Please note the ops includes " + str(self.ops.keys()) + ", and you can fetch them by getters")
    def text_analysis_one_list(self, op, list_of_num):
        median = statistics.median(list_of_num)
        list_of_num.sort()
        min_ = list_of_num[0]
        max_ = list_of_num[-1]
        print("op: " + op + " size: " + str(len(list_of_num)) + " median: " + str(median) + " min: " + str(min_) + " max: " + str(max_))
        return median

    def plot(self,op, list_of_num):
        target_filename = self.filename + ".op_" + op + ".png"
        self.debug_print("plot: target_filename: " + target_filename)
        data_size=len(list_of_num)
        data_set=sorted(set(list_of_num))
        bins=np.append(data_set, data_set[-1]+1)
        counts, bin_edges = np.histogram(list_of_num, bins=bins, density=False)
        counts=counts.astype(float)/data_size

        cdf = np.cumsum(counts)

        plt.plot(bin_edges[0:-1], cdf,linestyle='--', color='b') 
        plt.ylim((0,1))
        plt.ylabel("CDF")
        plt.grid(True)
        plt.xlabel('Latency (ms)')
        plt.xscale('log')
        plt.savefig(target_filename, dpi = 300)
        plt.clf()
    def analysis(self):
        #print(str(self.ops))
        list_of_all = []
        for op in self.ops:
            local_list = [i[0] for i in self.ops[op]]
            list_of_all.extend(local_list)
            self.res.append(self.text_analysis_one_list(op, local_list))
            #self.plot(op, local_list)
        self.res.append(self.text_analysis_one_list("all", list_of_all))
        self.all_lt = list_of_all
        #self.plot("all", list_of_all)
    def get_res(self):
        return self.res;
    def get_ops_of_time(self, time, span):
        # slow for loop for now
        result = []
        max_time = time + span*0.5
        min_time = time - span*0.5
        for optype in self.ops:
            for op in self.ops[optype]:
                time_stamp = op[1]
                if(time_stamp > min_time and time_stamp < max_time):
                    result.append(op)
        return result
    def get_span(self):
        # in case of OOO timestamp or something
        list_of_all = []
        for op in self.ops:
            local_list = [i[1] for i in self.ops[op]]
            list_of_all.extend(local_list)
        return (list_of_all[0], list_of_all[-1])

    def get_all_lt(self):
        return self.all_lt
    def get_op_lt(self, ops):
        result_list = []
        print("get_op_lt + " + str(ops))
        print(self.ops.keys())
        for op in ops:
            local_list = [i[0] for i in self.ops[op]]
            result_list.extend(local_list)
        return result_list



