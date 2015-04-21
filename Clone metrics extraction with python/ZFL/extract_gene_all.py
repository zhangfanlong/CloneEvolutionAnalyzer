#!/usr/bin/python
# -*- coding: utf-8 -*-

#作用：抽取克隆家系的度量值,使用慈萌和李智超结果
#使用方法:/usr/bin/python  pyname.py path1 path2
#结果:生成克隆家系的度量矩阵并写入arff文件中

#extract_gene.py path\GenealogyFiles\blocks\ path\gene_arff_result\

import re, sys, os
import xml.dom.minidom
import arff

#读取文件夹下的xml文件
file_list = os.listdir(sys.argv[1])
outputpath = sys.argv[2]

#打印文件列表

Metric = []
Matrix = []

#gb2312 -> utf-8
def convert_ecoding(file):
	f=open(file,'r').read()
	f=f.replace('<?xml version="1.0" encoding="gb2312"?>','<?xml version="1.0" encoding="utf-8"?>')
	f=unicode(f,encoding='gb2312').encode('utf-8')
	return f

def mkdir(path):
   
    # 去除首位空格
    path=path.strip()
    # 去除尾部 \ 符号
    path=path.rstrip("\\")
 
    # 判断路径是否存在
    # 存在     True
    # 不存在   False
    isExists=os.path.exists(path)
 
    # 判断结果
    if not isExists:
        # 如果不存在则创建目录
        #print path+' 创建成功'
        # 创建目录操作函数
        os.makedirs(path)
        return True
    else:
        # 如果目录存在则不创建，并提示目录已存在
        #print path+' 目录已存在'
        return False
	
#抽取目录中每一个克隆家系的度量值
for filename in file_list:
	#编码转换
	f=convert_ecoding(sys.argv[1] + filename)
	
	genedoc = xml.dom.minidom.parseString(f) 
	generoot = genedoc.documentElement
	gene_info_nodes = generoot.getElementsByTagName('GenealogyInfo')	
	if len(gene_info_nodes) != 0:
		gene_info_node=gene_info_nodes[0]
		#id
		id=int(gene_info_node.getAttribute('id'))
		#start
		#start=str(gene_info_node.getAttribute('startversion'))
		#end
		#end=str(gene_info_node.getAttribute('endversion'))
		#age
		age=int(gene_info_node.getAttribute('age'))
		
		evolu_pattern=generoot.getElementsByTagName('EvolutionPatternCount')[0] 
		#Static
		static=int(evolu_pattern.getAttribute('STATIC'))
		#same
		same=int(evolu_pattern.getAttribute('SAME'))
		#add
		add=int(evolu_pattern.getAttribute('ADD'))
		#delete
		delete=int(evolu_pattern.getAttribute('DELETE'))
		#consistent
		consistent=int(evolu_pattern.getAttribute('CONSISTENTCHANGE'))
		#inconsistent
		inconsistent=int(evolu_pattern.getAttribute('INCONSISTENTCHANGE'))
		#split
		split=int(evolu_pattern.getAttribute('SPLIT'))
		
		#生成克隆家系的度量向量 
		#Metric=[id,start,end,age,static,same,add,delete,consistent,inconsistent,split]
		#因聚类无法处理str类型，删除初始版本和结束版本信息
		Metric=[id,age,static,same,add,delete,consistent,inconsistent,split]
		#将每一个度量向量添加到度量矩阵
		Matrix.append(Metric)
	#else:
	#	continue
	#	single_info_node=generoot.getElementsByTagName('SingleCgGenealogy')	
	
#将Matric写入文件中,默认GenealogyFiles\
#arff.dump(outputpath + 'clone_geneaology_result.arff', Matrix, relation="clone_geneaology_matrics", names=['id', 'start', 'end', 'age', 'Static', 'same', 'add', 'delete', 'consistent' ,'inconsistent', 'split'])
#print 'Succeed!'
	
#将Matric写入文件中,默认GenealogyFiles\
删除初始版本和结束版本信息
mkdir(outputpath)
arff.dump(outputpath + 'clone_geneaology_result.arff', Matrix, relation="clone_geneaology_matrics", names=['id', 'age', 'Static', 'same', 'add', 'delete', 'consistent' ,'inconsistent', 'split'])
print 'Succeed!'