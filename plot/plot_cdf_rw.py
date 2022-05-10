import matplotlib.pyplot as plt
import numpy as np
import time
import sys
import re
from collections import defaultdict
import statistics

from latency_analyzer import *

def plot(op, list_of_num, color, linestyle):
    data_size=len(list_of_num)
    data_set=sorted(set(list_of_num))
    bins=np.append(data_set, data_set[-1]+1)
    counts, bin_edges = np.histogram(list_of_num, bins=bins, density=False)
    counts=counts.astype(float)/data_size
    cdf = np.cumsum(counts)
    ax1.plot(bin_edges[0:-1], cdf,linestyle=linestyle, label = op, color = color)

def drop_largest_with_percentage(list_of_num, tail_p):
    list_of_num.sort(key=float)
    slice_point = int(len(list_of_num) - len(list_of_num)/100.0*tail_p)
    return list_of_num[:slice_point]

if __name__ == "__main__":
    
    # hardcode values...
    # 0-100
    drop_tail_latency_percentage = 1

    if len(sys.argv) < 2:
        raise ValueError('wrong arg')
    with open(sys.argv[1]) as file:
        lines = file.readlines()
    fig = plt.figure()
    ax1 = fig.add_subplot(111)
    
    for line in lines:
        cols = line.split()
        print("lt file: " + cols[0] + " label: " + cols[1] + " op: " + cols[2] + " style: " + cols[3])
        style = cols[3].split(',')
        if(cols[2] ==  "all"):
            plot(cols[1], drop_largest_with_percentage(latency_analyzer(cols[0]).get_all_lt(), drop_tail_latency_percentage), color = style[0], linestyle = style[1])
        else:
            print("Parsing the requested ops and plot accordingly")
            ops = cols[2].split(",")
            print(ops)
            plot(cols[1], drop_largest_with_percentage(latency_analyzer(cols[0]).get_op_lt(ops), drop_tail_latency_percentage), color = style[0], linestyle = style[1])

    plt.xlabel("Latency (ms)")
    plt.ylabel("CDF")
    plt.grid(True)
    #plt.xscale('log')
    plt.ylim((0,1))
    plt.legend(loc='best');
    plt.savefig("./cdf.png", dpi = 300)

