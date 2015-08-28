#!/usr/bin/python
# -*- coding: cp936 -*-
import re, sys, os
import xml.dom.minidom
import math

#提取具有全部属性组的样本
#典型命令如下：
#C:\Users\founder\extract.py E:\论文资料\5实验系统\wget-results\CRDFiles\blocks\ E:\论文资料\5实验系统\wget-results\MAPFiles\blocks\

nclones = []#每个版本的克隆代码个数
LOC = []#克隆粒度
DBT = []#文件分布
SIM = []#克隆相似度
NOP = []#参数个数
CTX = []#上下文信息
NOT = []#操作符个数
NOD = []#操作数个数
UOT = []#操作符种类
UOD = []#操作数种类
LEN = []#程序长度
VCB = []#词汇量
VOL = []#容量
LV = []#水平
DIF = []#难度
CON = []#计算程序长度
EFF = []#努力度
PT = []#程序时间
EP  = []#进化模式
AGE = []#克隆寿命
CC  = []#变化复杂度
TG  = []#目标属性

#读取crd文件夹下的xml文件
file_list1 = os.listdir(sys.argv[1])

#函数：求给定版本区间（a,b）内的克隆代码总数
def num_of_clones(a, b):
        file_list = file_list1[a:b]
        num = 0
        for filename in file_list:
                doc = xml.dom.minidom.parse(sys.argv[1] + filename) 
                root = doc.documentElement
                class_nodes = root.getElementsByTagName('class')
                for classNode in class_nodes:
                        num = num + int(classNode.getAttribute('nclones'))
        return num

#静态度量的提取
for filename1 in file_list1:
	doc1 = xml.dom.minidom.parse(sys.argv[1] + filename1) 
	root1 = doc1.documentElement
	source_nodes = root1.getElementsByTagName('source')
	for sourceNode in source_nodes:
                #克隆粒度
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))
		LOC.append(str(e - s))
		#Halstead度量
		uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
		uotvalue = int(uotNode.childNodes[0].nodeValue)
		UOT.append(str(uotvalue))
		uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
		uodvalue = int(uodNode.childNodes[0].nodeValue)
		UOD.append(str(uodvalue))
		notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
		notvalue = int(notNode.childNodes[0].nodeValue)
		NOT.append(str(notvalue))
		nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
		nodvalue = int(nodNode.childNodes[0].nodeValue)
		NOD.append(str(nodvalue))
		N = notvalue + nodvalue
		C = uotvalue + uodvalue
		V = N * math.log(float(C), 2)
		L = (2 * uodvalue) / float(uotvalue * nodvalue)
		D = 1 / float(L)
		I = L * V
		E = V * D
		T = float(E) / 18
		LEN.append(str(N))
		VCB.append(str(C))
		VOL.append(str(V))
		LV.append(str(L))
		DIF.append(str(D))
		CON.append(str(I))
		EFF.append(str(E))
		PT.append(str(T))
		#参数个数
		if sourceNode.getElementsByTagName('methodInfo') == []:
                        NOP.append('0')
		else:
                        NOP.append(sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum'))
        	#上下文信息，默认为定义
				context = 'DEF'
		flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
		if flag == 0:
			btype_nodes = sourceNode.getElementsByTagName('bType')
			l = len(btype_nodes)
			btype_real = btype_nodes[l - 1]
			context = btype_real.childNodes[0].nodeValue
		CTX.append(context)
	class_nodes = root1.getElementsByTagName('class')
	num = 0
	#克隆群提取的信息
	for classNode in class_nodes:
                #每个版本每个克隆群的克隆代码个数
		n = int(classNode.getAttribute('nclones'))
		#克隆相似度
		similarity = int(classNode.getAttribute('similarity'))
		similarity = float(similarity) / 100
		source_nodes = classNode.getElementsByTagName('source')
		#文件分布
		files = []
		for sourceNode in source_nodes:
			files.append(sourceNode.getAttribute('file'))
			same = '1' if len(set(files)) == 1 else '0'
		while n > 0:
			DBT.append(same)
			SIM.append(str(similarity))
			n = n - 1
		num = num + int(classNode.getAttribute('nclones'))
	nclones.append(num)#所有版本克隆代码个数，即样本大小
#设置初始版本（第0版本）的各进化度量默认值
for i in range(1, int(nclones[0]) + 1):
	TG.append(1)
	EP.append(1)
	AGE.append('1')
	CC.append('0')
#读取map文件下的xml文件
file_list2 = os.listdir(sys.argv[2])
#进化度量提取
for index in range(0, len(file_list2)):
	doc2 = xml.dom.minidom.parse(sys.argv[2] + file_list2[index]) 
	root2 = doc2.documentElement
	#映射上的克隆群节点
	cgmap_nodes = root2.getElementsByTagName('CGMap')
	#未映射的克隆群节点
	unmappeddestcg_nodes = root2.getElementsByTagName('UnMappedDestCG')
	#未映射的克隆代码片段节点
	unmappeddestcf_nodes = []
	if unmappeddestcg_nodes != []:
		unmappeddestcf_nodes = unmappeddestcg_nodes[0].getElementsByTagName('CGInfo')
	#对于每个目标克隆群
	for destCGid in range(1, int(nclones[index + 1]) + 1):
		for cgmapnode in cgmap_nodes:
			if int(cgmapnode.getAttribute('destCGid')) == destCGid:
                                #进化模式与有害性的默认值
				pattern = 1
				harmful = 1
				#获取进化模式信息
				epnode = cgmapnode.getElementsByTagName('EvolutionPattern')[0]
				if epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
					pattern = 3
				elif epnode.getAttribute('CONSISTENTCHANGE') == 'True':
					pattern = 2
					harmful = 0#出现 一致变化，则为有害克隆
				CGid = int(cgmapnode.getAttribute('srcCGid'))
				#往之前版本回溯
				for i in range(index - 1, -1 ,-1):
					doc = xml.dom.minidom.parse(sys.argv[2] + file_list2[i]) 
					root = doc.documentElement
					old_cgmap_nodes = root.getElementsByTagName('CGMap')
					found = 0
					for old_cgmapnode in old_cgmap_nodes:
                                                #如果前一个版本中有祖先
						if int(old_cgmapnode.getAttribute('destCGid')) == CGid:
							found = 1
							old_pattern = 1
							old_epnode = old_cgmapnode.getElementsByTagName('EvolutionPattern')[0]
							if old_epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
								old_pattern = 3
							elif old_epnode.getAttribute('CONSISTENTCHANGE') == 'True':
								old_pattern = 2
								harmful = 0 
							if pattern < old_pattern:
								pattern = old_pattern
							CGid = int(old_cgmapnode.getAttribute('srcCGid'))
					if found == 0:
						break
				CGid = int(cgmapnode.getAttribute('srcCGid'))
				#对于克隆群中每一个克隆代码片段
				cfmap_nodes = cgmapnode.getElementsByTagName('CFMap')
				destCGsize = int(cgmapnode.getAttribute('destCGsize'))
				for destCFid in range(1, destCGsize + 1):
					EP.append(pattern)
					TG.append(harmful)
					ffound = 0
					for cfmapnode in cfmap_nodes:
						if int(cfmapnode.getAttribute('destCFid')) == destCFid:
							ffound = 1
							lifetime = 1
							changetime = 0
							CFid = int(cfmapnode.getAttribute('srcCFid'))
							old_CGid = CGid
							#往之前版本回溯求克隆寿命与变化复杂度
							for i in range(index - 1, -1, -1):
								doc = xml.dom.minidom.parse(sys.argv[2] + file_list2[i])
								root = doc.documentElement
								old_cgmap_nodes = root.getElementsByTagName('CGMap')
								aimCGid = old_CGid
								gfound = 0
								for old_cgmapnode in old_cgmap_nodes:
									if int(old_cgmapnode.getAttribute('destCGid')) == aimCGid:
										old_cfmap_nodes = old_cgmapnode.getElementsByTagName('CFMap')
										old_CGid = int(old_cgmapnode.getAttribute('srcCGid'))
										for old_cfmapnode in old_cfmap_nodes:
                                                                                        #在前一个版本的克隆群中找到祖先，寿命加1
											if int(old_cfmapnode.getAttribute('destCFid')) == CFid:
												gfound = 1
												lifetime = lifetime + 1
												#相似度不为1，即发生了改变，变化复杂度加1
												if old_cfmapnode.getAttribute('textSim') != '1':
													changetime = changetime + 1
												CFid = int(old_cfmapnode.getAttribute('srcCFid'))
								if gfound == 0:
									break
							AGE.append(str(lifetime))
							CC.append(str(changetime))
					if ffound == 0:
						AGE.append('1')
						CC.append('0')
        	#未映射的克隆代码片段，则为新生克隆，寿命为1，变化复杂度为0，有害性为无害
		for unmappeddestcfnode in unmappeddestcf_nodes:
			if int(unmappeddestcfnode.getAttribute('id')) == destCGid:
				destCGsize = int(unmappeddestcfnode.getAttribute('size'))
				for i in range(0, destCGsize):
					TG.append(1)
					EP.append(1)
					AGE.append('1')
					CC.append('0')

#求各特征信息增益，只在全部特征组的情况下计算
IG_all = 0
IG_LOC = 0
IG_Hal = 0
IG_Sim = 0
IG_nParam = 0
IG_context = 0
IG_FD = 0
IG_Age = 0
IG_CC = 0
positive_num = 0
negtive_num = 0
total_num = len(TG)
uLOC = []
splitLOC = []
uTG = []
#样本总体信息熵
for item in TG:
        if item == 0:
                positive_num = positive_num + 1
        elif item == 1:
                negtive_num = negtive_num + 1
IG_all = - (positive_num / float(total_num)) * math.log((positive_num / float(total_num)), 2) - (negtive_num / float(total_num)) * math.log((negtive_num / float(total_num)), 2)
#函数：求给定特征feature的信息熵（连续值）
def IG(feature):
        unique = []
        split = []
        for item in set(feature):
                unique.append(float(item))
        unique.sort()
        for i in range(0, len(unique) - 1):
                split.append(float(unique[i] + unique[i+1])/2)
        IG_best = 100
        for i in range(0, len(split)):
                pot = split[i]
                phigh = 0#大于分裂点的正例个数
                nhigh = 0#大于分裂点的负例个数
                plow = 0#小于分裂点的正例个数
                nlow = 0#小于分裂点的负例个数
                for j in range(0, len(feature)):
                        if float(feature[j]) > pot:
                                if TG[j] == 0:
                                        phigh = phigh + 1
                                elif TG[j] == 1:
                                        nhigh = nhigh + 1
                        else:
                                if TG[j] == 0:
                                        plow = plow + 1
                                elif TG[j] == 1:
                                        nlow = nlow + 1
                thigh = phigh + nhigh
                tlow = plow + nlow
                #公式太长，拆成4个部分
                if phigh == 0:
                        IG_phigh = 0
                else:
                        IG_phigh = -(phigh/float(thigh))*math.log((phigh/float(thigh)),2)
                if nhigh == 0:
                        IG_nhigh = 0
                else:
                        IG_nhigh = -(nhigh/float(thigh))*math.log((nhigh/float(thigh)),2)
                IG_high = thigh/float(thigh+tlow)*(IG_phigh+IG_nhigh)
                if plow == 0:
                        IG_plow = 0
                else:
                        IG_plow = -(plow/float(tlow))*math.log((plow/float(tlow)),2)
                if nlow == 0:
                        IG_nlow = 0
                else:
                        IG_nlow = -(nlow/float(tlow))*math.log((nlow/float(tlow)),2)
                IG_low = tlow/float(thigh+tlow)*(IG_plow+IG_nlow)
                IG_temp = IG_high + IG_low
                #选择最小期望的信息熵
                if IG_temp < IG_best:
                        IG_best = IG_temp
        return IG_best
IGCTX = []#CTX为字符串，为求信息熵，转化为离散整数值
for item in CTX:
	if item == 'DEF':
		IGCTX.append(1)
	elif item == 'IF' or item == 'ELSE' or item == 'SWITCH':
		IGCTX.append(2)
	elif item == 'FOR' or 'WHILE' or 'DO':
		IGCTX.append(3)
	else:
		IGCTX.append(4)
#计算各特征信息增益
IG_LOC = IG_all - IG(LOC)
IG_Sim = IG_all - IG(SIM)
IG_nParam = IG_all - IG(NOP)
IG_FD = IG_all - IG(DBT)
IG_Age = IG_all - IG(AGE)
IG_CC = IG_all - IG(CC)
IG_Hal = (4*IG_all - IG(NOT) - IG(NOD) - IG(UOT) - IG(UOD))/4
IG_context = IG_all - IG(IGCTX)
#打印信息增益结果
print('Information Gain:')
print('+++++++++++++++++++++++++++++++')
print('1.IG of LOC             : ' + str(IG_LOC))
print('2.IG of Halstead        : ' + str(IG_Hal))
print('3.IG of Similarity      : ' + str(IG_Sim))
print('4.IG of #Params         : ' + str(IG_nParam))
print('5.IG of Context         : ' + str(IG_context))
print('6.IG of FileDistribution: ' + str(IG_FD))
print('7.IG of Age             : ' + str(IG_Age))
print('8.IG of ChangeComplexity: ' + str(IG_CC))

#写样本文件，将各特征按libsvm指定格式写入
LOC_for_svm = []
DBT_for_svm = []
CTX_for_svm = []
NOT_for_svm = []
NOD_for_svm = []
UOT_for_svm = []
UOD_for_svm = []
EP_for_svm  = []
AGE_for_svm = []
CC_for_svm  = []
TG_for_svm  = []
SIM_for_svm = []
NOP_for_svm = []
LEN_for_svm = []
VCB_for_svm = []
VOL_for_svm = []
LV_for_svm = []
DIF_for_svm = []
CON_for_svm = []
EFF_for_svm = []
PT_for_svm = []
#在各特征前加序号
for item in TG:
	if item == 1:
		item = '-1'
	elif item == 0:
		item = '+1'
	TG_for_svm.append(item)
for item in LOC:
	item = ' 1:' + item
	LOC_for_svm.append(item)
for item in DBT:
	item = ' 2:' + item
	DBT_for_svm.append(item)
for item in CTX:
	if item == 'DEF':
		item = ' 3:1 4:0 5:0 6:0'
	elif item == 'IF' or item == 'ELSE' or item == 'SWITCH':
		item = ' 3:0 4:1 5:0 6:0'
	elif item == 'FOR' or 'WHILE' or 'DO':
		item = ' 3:0 4:0 5:1 6:0'
	else:
		item = ' 3:0 4:0 5:0 6:0'
	CTX_for_svm.append(item)
for item in NOT:
	item = ' 7:' + item
	NOT_for_svm.append(item)
for item in NOD:
	item = ' 8:' + item
	NOD_for_svm.append(item)
for item in UOT:
	item = ' 9:' + item
	UOT_for_svm.append(item)
for item in UOD:
	item = ' 10:' + item
	UOD_for_svm.append(item)
for item in AGE:
	item = ' 11:' + item
	AGE_for_svm.append(item)
for item in CC:
	item = ' 12:' + item
	CC_for_svm.append(item)
for item in SIM:
        item = ' 13:' + item
        SIM_for_svm.append(item)
for item in NOP:
        item = ' 14:' + item
        NOP_for_svm.append(item)
for item in LEN:
        item = ' 15:' + item
        LEN_for_svm.append(item)
for item in VCB:
        item = ' 16:' + item
        VCB_for_svm.append(item)
for item in VOL:
        item = ' 17:' + item
        VOL_for_svm.append(item)
for item in LV:
        item = ' 18:' + item
        LV_for_svm.append(item)
for item in DIF:
        item = ' 19:' + item
        DIF_for_svm.append(item)
for item in CON:
        item = ' 20:' + item
        CON_for_svm.append(item)
for item in EFF:
        item = ' 21:' + item
        EFF_for_svm.append(item)
for item in PT:
        item = ' 22:' + item
        PT_for_svm.append(item)
#已废除的进化模式特征
'''for item in EP:
	if item == 1:
		item = ' 13:1 14:0 15:0'
	elif item == 2:
		item = ' 13:0 14:1 15:0'
	else:
		item = ' 13:0 14:0 15:1'
	EP_for_svm.append(item)'''
train_file = open('train', 'w')
test_file = open('test', 'w')
zipped = zip(TG_for_svm, LOC_for_svm, DBT_for_svm, CTX_for_svm, NOT_for_svm, NOD_for_svm, UOT_for_svm, UOD_for_svm,
             AGE_for_svm, CC_for_svm, SIM_for_svm, NOP_for_svm, LEN_for_svm, VCB_for_svm, VOL_for_svm, LV_for_svm, DIF_for_svm, CON_for_svm, EFF_for_svm, PT_for_svm)
#zipped = zip(TG_for_svm, EP_for_svm)
k = list(zipped)
#对于wget系统，训练集版本周期0-7，测试集版本周期8-9
num_of_trainset = num_of_clones(0, 7)
for i in range(0, num_of_trainset):
	train_file.writelines(k[i])
	train_file.write('\n')
for i in range(num_of_trainset, len(LOC_for_svm)):
        test_file.writelines(k[i])
        test_file.write('\n')
train_file.close()
test_file.close()
