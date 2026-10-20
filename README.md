# Execl读表脚本工具(Editor)

*外部工具：*  ExcelDataReader(Github)、ExcelDataReader.DataSet（Github）


## 操作流程
### Excel
- *step1* Unity Editor工具栏位Tool，点击- **获取Excel目录** -拿到存储Excel目录对应的文件夹
- *step2* 点击Tool工具 - **处理所有的Excel文件** -自动处理Excel文件，具体生成于Generate文件夹中

## Execl规范
### 第一行
	默认为一个表的对数据的描述名，比如id, name, number
### 第二行
	默认为变量类型，比如id对应的是int, name对应string
### 第三行
	默认为描述名的介绍，不放入脚本工具的自动生成栏中
### 第四栏及以下
	Excel表中真正的数据

| A    | B      | C      | D        | E      |
| ---- | ------ | ------ | -------- | ------ |
| id   | value1 | MCount | formalua | text   |
| int  | float  | int    | int[]    | string |
| 物品ID | 物品数值   | 最大数量   | 合成配方     | 介绍     |
| 1    | 2.3    | 6      | 1,2,3    | herro  |
| 2    | 4.56   | 6      | 52,11,22 | 2wcawd |
| 3    | 1.2    | 11     | 1,112,3  | scac   |
| 4    | 10.22  | 1313   | 2,2,2    | zxczx  |
## Tools生成工具
*流程*

一个Excel文件，需要对其转换成数据流，然后转换成unity可读的Execl的形式，其中，按照规范，重点取第一行，第二行，第四行及以下的数据，其中，

	将第一行和第二行一一配对，生成第一个脚本封存为数据结构，生成第二个脚本，其中第四行以下所有的数据封装为二进制文件bytes,其中脚本名字为{Excel.Name}Config.cs二进制文件名为{Excel.Name}.bytes
	
	内部封存二进制文件名，通过文件名找到二进制数据文件，将第一行开头设置为key,通过这个key获取二进制数据匹配的对应数据类
	
	将第一个脚本封装的数据类一一对应，按顺序读取和写入即可编写一个二进制与unity序列化文件写入库
	
对于配置表来说，一个配置表需要生成三个脚本

*第一个脚本* 是这个表每一条数据的数据结构，以class包裹，内部按表中固定规范的第二行变量名＋第一行描述名以属性的形式依次生成，也就是类似于 public Id {get; set;}

*第二个脚本* 是将Tool读取的Excel表中的所有数据转存的二进制文件bytes转换成unity可识别的unity资产文件，并且通过第一个脚本的id匹配对应的class数据类

*第三个脚本* 如果我需要直接对二进制文件进行操作，绕过第二个脚本转换操作。
