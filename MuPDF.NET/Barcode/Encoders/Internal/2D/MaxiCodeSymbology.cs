/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using System.Drawing;
using System.Collections;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using MaxiCode symbology.
    /// </summary>
    class MaxiCodeSymbology : SymbologyDrawing2D
    {
        private const int first_message_length = 10; // +10 symbols of error correction
        private const int second_message_length = 124; // total length along with error correction symbols
        private const int sec_length = 40;
        private const int eec_length = 56;

        private static SKPoint[] c_orientationTemplate = new SKPoint[]
        {
            new SKPoint(00,28), new SKPoint(00,29), new SKPoint(09,10), new SKPoint(09,11), new SKPoint(10,11), new SKPoint(15,07), new SKPoint(16,08),
            new SKPoint(16,20), new SKPoint(17,20), new SKPoint(22,10), new SKPoint(23,10), new SKPoint(22,17), new SKPoint(23,17)
        };

        private static SKPoint[][] c_symbolMap = new SKPoint[][]
        {
            new SKPoint[] {new SKPoint(15,19), new SKPoint(17,19), new SKPoint(09,16), new SKPoint(10,16), new SKPoint(11,17), new SKPoint(11,16)}, // 01
            new SKPoint[] {new SKPoint(22,13), new SKPoint(22,12), new SKPoint(23,13), new SKPoint(23,12), new SKPoint(21,17), new SKPoint(22,16)}, // 02
            new SKPoint[] {new SKPoint(09,13), new SKPoint(09,12), new SKPoint(10,13), new SKPoint(10,12), new SKPoint(12,10), new SKPoint(20,10)}, // 03
            new SKPoint[] {new SKPoint(20,18), new SKPoint(12,19), new SKPoint(12,18), new SKPoint(13,19), new SKPoint(13,18), new SKPoint(14,19)}, // 04
            new SKPoint[] {new SKPoint(23,15), new SKPoint(23,14), new SKPoint(18,19), new SKPoint(19,19), new SKPoint(19,18), new SKPoint(20,19)}, // 05
            new SKPoint[] {new SKPoint(15,08), new SKPoint(17,08), new SKPoint(21,10), new SKPoint(23,11), new SKPoint(22,15), new SKPoint(22,14)}, // 06
            new SKPoint[] {new SKPoint(09,15), new SKPoint(09,14), new SKPoint(10,15), new SKPoint(10,14), new SKPoint(10,10), new SKPoint(11,10)}, // 07
            new SKPoint[] {new SKPoint(17,21), new SKPoint(09,19), new SKPoint(09,18), new SKPoint(10,19), new SKPoint(11,19), new SKPoint(11,18)}, // 08
            new SKPoint[] {new SKPoint(15,06), new SKPoint(16,06), new SKPoint(17,07), new SKPoint(17,06), new SKPoint(15,21), new SKPoint(15,20)}, // 09
            new SKPoint[] {new SKPoint(12,09), new SKPoint(12,08), new SKPoint(13,09), new SKPoint(13,08), new SKPoint(14,09), new SKPoint(14,08)}, // 10
            new SKPoint[] {new SKPoint(18,09), new SKPoint(18,08), new SKPoint(19,09), new SKPoint(19,08), new SKPoint(20,09), new SKPoint(20,08)}, // 11
            new SKPoint[] {new SKPoint(21,19), new SKPoint(21,18), new SKPoint(22,19), new SKPoint(22,18), new SKPoint(23,19), new SKPoint(23,18)}, // 12
            new SKPoint[] {new SKPoint(21,09), new SKPoint(21,08), new SKPoint(22,09), new SKPoint(22,08), new SKPoint(23,09), new SKPoint(23,08)}, // 13
            new SKPoint[] {new SKPoint(09,09), new SKPoint(09,08), new SKPoint(10,09), new SKPoint(10,08), new SKPoint(11,09), new SKPoint(11,08)}, // 14
            new SKPoint[] {new SKPoint(12,21), new SKPoint(12,20), new SKPoint(13,21), new SKPoint(13,20), new SKPoint(14,21), new SKPoint(14,20)}, // 15
            new SKPoint[] {new SKPoint(18,21), new SKPoint(18,20), new SKPoint(19,21), new SKPoint(19,20), new SKPoint(20,21), new SKPoint(20,20)}, // 16
            new SKPoint[] {new SKPoint(18,07), new SKPoint(18,06), new SKPoint(19,07), new SKPoint(19,06), new SKPoint(20,07), new SKPoint(20,06)}, // 17
            new SKPoint[] {new SKPoint(12,07), new SKPoint(12,06), new SKPoint(13,07), new SKPoint(13,06), new SKPoint(14,07), new SKPoint(14,06)}, // 18
            new SKPoint[] {new SKPoint(09,21), new SKPoint(09,20), new SKPoint(10,21), new SKPoint(10,20), new SKPoint(11,21), new SKPoint(11,20)}, // 19
            new SKPoint[] {new SKPoint(21,21), new SKPoint(21,20), new SKPoint(22,21), new SKPoint(22,20), new SKPoint(23,21), new SKPoint(23,20)}, // 20
            new SKPoint[] {new SKPoint(00,01), new SKPoint(00,00), new SKPoint(01,01), new SKPoint(01,00), new SKPoint(02,01), new SKPoint(02,00)}, // 21
            new SKPoint[] {new SKPoint(00,03), new SKPoint(00,02), new SKPoint(01,03), new SKPoint(01,02), new SKPoint(02,03), new SKPoint(02,02)}, // 22
            new SKPoint[] {new SKPoint(00,05), new SKPoint(00,04), new SKPoint(01,05), new SKPoint(01,04), new SKPoint(02,05), new SKPoint(02,04)}, // 23
            new SKPoint[] {new SKPoint(00,07), new SKPoint(00,06), new SKPoint(01,07), new SKPoint(01,06), new SKPoint(02,07), new SKPoint(02,06)}, // 24
            new SKPoint[] {new SKPoint(00,09), new SKPoint(00,08), new SKPoint(01,09), new SKPoint(01,08), new SKPoint(02,09), new SKPoint(02,08)}, // 25
            new SKPoint[] {new SKPoint(00,11), new SKPoint(00,10), new SKPoint(01,11), new SKPoint(01,10), new SKPoint(02,11), new SKPoint(02,10)}, // 26
            new SKPoint[] {new SKPoint(00,13), new SKPoint(00,12), new SKPoint(01,13), new SKPoint(01,12), new SKPoint(02,13), new SKPoint(02,12)}, // 27
            new SKPoint[] {new SKPoint(00,15), new SKPoint(00,14), new SKPoint(01,15), new SKPoint(01,14), new SKPoint(02,15), new SKPoint(02,14)}, // 28
            new SKPoint[] {new SKPoint(00,17), new SKPoint(00,16), new SKPoint(01,17), new SKPoint(01,16), new SKPoint(02,17), new SKPoint(02,16)}, // 29
            new SKPoint[] {new SKPoint(00,19), new SKPoint(00,18), new SKPoint(01,19), new SKPoint(01,18), new SKPoint(02,19), new SKPoint(02,18)}, // 30
            new SKPoint[] {new SKPoint(00,21), new SKPoint(00,20), new SKPoint(01,21), new SKPoint(01,20), new SKPoint(02,21), new SKPoint(02,20)}, // 31
            new SKPoint[] {new SKPoint(00,23), new SKPoint(00,22), new SKPoint(01,23), new SKPoint(01,22), new SKPoint(02,23), new SKPoint(02,22)}, // 32
            new SKPoint[] {new SKPoint(00,25), new SKPoint(00,24), new SKPoint(01,25), new SKPoint(01,24), new SKPoint(02,25), new SKPoint(02,24)}, // 33
            new SKPoint[] {new SKPoint(00,27), new SKPoint(00,26), new SKPoint(01,27), new SKPoint(01,26), new SKPoint(02,27), new SKPoint(02,26)}, // 34
            new SKPoint[] {new SKPoint(03,27), new SKPoint(03,26), new SKPoint(04,27), new SKPoint(04,26), new SKPoint(05,27), new SKPoint(05,26)}, // 35
            new SKPoint[] {new SKPoint(03,25), new SKPoint(03,24), new SKPoint(04,25), new SKPoint(04,24), new SKPoint(05,25), new SKPoint(05,24)}, // 36
            new SKPoint[] {new SKPoint(03,23), new SKPoint(03,22), new SKPoint(04,23), new SKPoint(04,22), new SKPoint(05,23), new SKPoint(05,22)}, // 37
            new SKPoint[] {new SKPoint(03,21), new SKPoint(03,20), new SKPoint(04,21), new SKPoint(04,20), new SKPoint(05,21), new SKPoint(05,20)}, // 38
            new SKPoint[] {new SKPoint(03,19), new SKPoint(03,18), new SKPoint(04,19), new SKPoint(04,18), new SKPoint(05,19), new SKPoint(05,18)}, // 39
            new SKPoint[] {new SKPoint(03,17), new SKPoint(03,16), new SKPoint(04,17), new SKPoint(04,16), new SKPoint(05,17), new SKPoint(05,16)}, // 40
            new SKPoint[] {new SKPoint(03,15), new SKPoint(03,14), new SKPoint(04,15), new SKPoint(04,14), new SKPoint(05,15), new SKPoint(05,14)}, // 41
            new SKPoint[] {new SKPoint(03,13), new SKPoint(03,12), new SKPoint(04,13), new SKPoint(04,12), new SKPoint(05,13), new SKPoint(05,12)}, // 42
            new SKPoint[] {new SKPoint(03,11), new SKPoint(03,10), new SKPoint(04,11), new SKPoint(04,10), new SKPoint(05,11), new SKPoint(05,10)}, // 43
            new SKPoint[] {new SKPoint(03,09), new SKPoint(03,08), new SKPoint(04,09), new SKPoint(04,08), new SKPoint(05,09), new SKPoint(05,08)}, // 44
            new SKPoint[] {new SKPoint(03,07), new SKPoint(03,06), new SKPoint(04,07), new SKPoint(04,06), new SKPoint(05,07), new SKPoint(05,06)}, // 45
            new SKPoint[] {new SKPoint(03,05), new SKPoint(03,04), new SKPoint(04,05), new SKPoint(04,04), new SKPoint(05,05), new SKPoint(05,04)}, // 46
            new SKPoint[] {new SKPoint(03,03), new SKPoint(03,02), new SKPoint(04,03), new SKPoint(04,02), new SKPoint(05,03), new SKPoint(05,02)}, // 47
            new SKPoint[] {new SKPoint(03,01), new SKPoint(03,00), new SKPoint(04,01), new SKPoint(04,00), new SKPoint(05,01), new SKPoint(05,00)}, // 48
            new SKPoint[] {new SKPoint(06,01), new SKPoint(06,00), new SKPoint(07,01), new SKPoint(07,00), new SKPoint(08,01), new SKPoint(08,00)}, // 49
            new SKPoint[] {new SKPoint(06,03), new SKPoint(06,02), new SKPoint(07,03), new SKPoint(07,02), new SKPoint(08,03), new SKPoint(08,02)}, // 50
            new SKPoint[] {new SKPoint(06,05), new SKPoint(06,04), new SKPoint(07,05), new SKPoint(07,04), new SKPoint(08,05), new SKPoint(08,04)}, // 51
            new SKPoint[] {new SKPoint(06,07), new SKPoint(06,06), new SKPoint(07,07), new SKPoint(07,06), new SKPoint(08,07), new SKPoint(08,06)}, // 52
            new SKPoint[] {new SKPoint(06,09), new SKPoint(06,08), new SKPoint(07,09), new SKPoint(07,08), new SKPoint(08,09), new SKPoint(08,08)}, // 53
            new SKPoint[] {new SKPoint(06,11), new SKPoint(06,10), new SKPoint(07,11), new SKPoint(07,10), new SKPoint(08,11), new SKPoint(08,10)}, // 54
            new SKPoint[] {new SKPoint(06,13), new SKPoint(06,12), new SKPoint(07,13), new SKPoint(07,12), new SKPoint(08,13), new SKPoint(08,12)}, // 55
            new SKPoint[] {new SKPoint(06,15), new SKPoint(06,14), new SKPoint(07,15), new SKPoint(07,14), new SKPoint(08,15), new SKPoint(08,14)}, // 56
            new SKPoint[] {new SKPoint(06,17), new SKPoint(06,16), new SKPoint(07,17), new SKPoint(07,16), new SKPoint(08,17), new SKPoint(08,16)}, // 57
            new SKPoint[] {new SKPoint(06,19), new SKPoint(06,18), new SKPoint(07,19), new SKPoint(07,18), new SKPoint(08,19), new SKPoint(08,18)}, // 58
            new SKPoint[] {new SKPoint(06,21), new SKPoint(06,20), new SKPoint(07,21), new SKPoint(07,20), new SKPoint(08,21), new SKPoint(08,20)}, // 59
            new SKPoint[] {new SKPoint(06,23), new SKPoint(06,22), new SKPoint(07,23), new SKPoint(07,22), new SKPoint(08,23), new SKPoint(08,22)}, // 60
            new SKPoint[] {new SKPoint(06,25), new SKPoint(06,24), new SKPoint(07,25), new SKPoint(07,24), new SKPoint(08,25), new SKPoint(08,24)}, // 61
            new SKPoint[] {new SKPoint(06,27), new SKPoint(06,26), new SKPoint(07,27), new SKPoint(07,26), new SKPoint(08,27), new SKPoint(08,26)}, // 62
            new SKPoint[] {new SKPoint(09,27), new SKPoint(09,26), new SKPoint(10,27), new SKPoint(10,26), new SKPoint(11,27), new SKPoint(11,26)}, // 63
            new SKPoint[] {new SKPoint(09,25), new SKPoint(09,24), new SKPoint(10,25), new SKPoint(10,24), new SKPoint(11,25), new SKPoint(11,24)}, // 64
            new SKPoint[] {new SKPoint(09,23), new SKPoint(09,22), new SKPoint(10,23), new SKPoint(10,22), new SKPoint(11,23), new SKPoint(11,22)}, // 65
            new SKPoint[] {new SKPoint(09,07), new SKPoint(09,06), new SKPoint(10,07), new SKPoint(10,06), new SKPoint(11,07), new SKPoint(11,06)}, // 66
            new SKPoint[] {new SKPoint(09,05), new SKPoint(09,04), new SKPoint(10,05), new SKPoint(10,04), new SKPoint(11,05), new SKPoint(11,04)}, // 67
            new SKPoint[] {new SKPoint(09,03), new SKPoint(09,02), new SKPoint(10,03), new SKPoint(10,02), new SKPoint(11,03), new SKPoint(11,02)}, // 68
            new SKPoint[] {new SKPoint(09,01), new SKPoint(09,00), new SKPoint(10,01), new SKPoint(10,00), new SKPoint(11,01), new SKPoint(11,00)}, // 69
            new SKPoint[] {new SKPoint(12,01), new SKPoint(12,00), new SKPoint(13,01), new SKPoint(13,00), new SKPoint(14,01), new SKPoint(14,00)}, // 70
            new SKPoint[] {new SKPoint(12,03), new SKPoint(12,02), new SKPoint(13,03), new SKPoint(13,02), new SKPoint(14,03), new SKPoint(14,02)}, // 71
            new SKPoint[] {new SKPoint(12,05), new SKPoint(12,04), new SKPoint(13,05), new SKPoint(13,04), new SKPoint(14,05), new SKPoint(14,04)}, // 72
            new SKPoint[] {new SKPoint(12,23), new SKPoint(12,22), new SKPoint(13,23), new SKPoint(13,22), new SKPoint(14,23), new SKPoint(14,22)}, // 73
            new SKPoint[] {new SKPoint(12,25), new SKPoint(12,24), new SKPoint(13,25), new SKPoint(13,24), new SKPoint(14,25), new SKPoint(14,24)}, // 74
            new SKPoint[] {new SKPoint(12,27), new SKPoint(12,26), new SKPoint(13,27), new SKPoint(13,26), new SKPoint(14,27), new SKPoint(14,26)}, // 75
            new SKPoint[] {new SKPoint(15,27), new SKPoint(15,26), new SKPoint(16,27), new SKPoint(16,26), new SKPoint(17,27), new SKPoint(17,26)}, // 76
            new SKPoint[] {new SKPoint(15,25), new SKPoint(15,24), new SKPoint(16,25), new SKPoint(16,24), new SKPoint(17,25), new SKPoint(17,24)}, // 77
            new SKPoint[] {new SKPoint(15,23), new SKPoint(15,22), new SKPoint(16,23), new SKPoint(16,22), new SKPoint(17,23), new SKPoint(17,22)}, // 78
            new SKPoint[] {new SKPoint(15,05), new SKPoint(15,04), new SKPoint(16,05), new SKPoint(16,04), new SKPoint(17,05), new SKPoint(17,04)}, // 79
            new SKPoint[] {new SKPoint(15,03), new SKPoint(15,02), new SKPoint(16,03), new SKPoint(16,02), new SKPoint(17,03), new SKPoint(17,02)}, // 80
            new SKPoint[] {new SKPoint(15,01), new SKPoint(15,00), new SKPoint(16,01), new SKPoint(16,00), new SKPoint(17,01), new SKPoint(17,00)}, // 81
            new SKPoint[] {new SKPoint(18,01), new SKPoint(18,00), new SKPoint(19,01), new SKPoint(19,00), new SKPoint(20,01), new SKPoint(20,00)}, // 82
            new SKPoint[] {new SKPoint(18,03), new SKPoint(18,02), new SKPoint(19,03), new SKPoint(19,02), new SKPoint(20,03), new SKPoint(20,02)}, // 83
            new SKPoint[] {new SKPoint(18,05), new SKPoint(18,04), new SKPoint(19,05), new SKPoint(19,04), new SKPoint(20,05), new SKPoint(20,04)}, // 84
            new SKPoint[] {new SKPoint(18,23), new SKPoint(18,22), new SKPoint(19,23), new SKPoint(19,22), new SKPoint(20,23), new SKPoint(20,22)}, // 85
            new SKPoint[] {new SKPoint(18,25), new SKPoint(18,24), new SKPoint(19,25), new SKPoint(19,24), new SKPoint(20,25), new SKPoint(20,24)}, // 86
            new SKPoint[] {new SKPoint(18,27), new SKPoint(18,26), new SKPoint(19,27), new SKPoint(19,26), new SKPoint(20,27), new SKPoint(20,26)}, // 87
            new SKPoint[] {new SKPoint(21,27), new SKPoint(21,26), new SKPoint(22,27), new SKPoint(22,26), new SKPoint(23,27), new SKPoint(23,26)}, // 88
            new SKPoint[] {new SKPoint(21,25), new SKPoint(21,24), new SKPoint(22,25), new SKPoint(22,24), new SKPoint(23,25), new SKPoint(23,24)}, // 89
            new SKPoint[] {new SKPoint(21,23), new SKPoint(21,22), new SKPoint(22,23), new SKPoint(22,22), new SKPoint(23,23), new SKPoint(23,22)}, // 90
            new SKPoint[] {new SKPoint(21,07), new SKPoint(21,06), new SKPoint(22,07), new SKPoint(22,06), new SKPoint(23,07), new SKPoint(23,06)}, // 91
            new SKPoint[] {new SKPoint(21,05), new SKPoint(21,04), new SKPoint(22,05), new SKPoint(22,04), new SKPoint(23,05), new SKPoint(23,04)}, // 92
            new SKPoint[] {new SKPoint(21,03), new SKPoint(21,02), new SKPoint(22,03), new SKPoint(22,02), new SKPoint(23,03), new SKPoint(23,02)}, // 93
            new SKPoint[] {new SKPoint(21,01), new SKPoint(21,00), new SKPoint(22,01), new SKPoint(22,00), new SKPoint(23,01), new SKPoint(23,00)}, // 94
            new SKPoint[] {new SKPoint(24,01), new SKPoint(24,00), new SKPoint(25,01), new SKPoint(25,00), new SKPoint(26,01), new SKPoint(26,00)}, // 95
            new SKPoint[] {new SKPoint(24,03), new SKPoint(24,02), new SKPoint(25,03), new SKPoint(25,02), new SKPoint(26,03), new SKPoint(26,02)}, // 96
            new SKPoint[] {new SKPoint(24,05), new SKPoint(24,04), new SKPoint(25,05), new SKPoint(25,04), new SKPoint(26,05), new SKPoint(26,04)}, // 97
            new SKPoint[] {new SKPoint(24,07), new SKPoint(24,06), new SKPoint(25,07), new SKPoint(25,06), new SKPoint(26,07), new SKPoint(26,06)}, // 98
            new SKPoint[] {new SKPoint(24,09), new SKPoint(24,08), new SKPoint(25,09), new SKPoint(25,08), new SKPoint(26,09), new SKPoint(26,08)}, // 99
            new SKPoint[] {new SKPoint(24,11), new SKPoint(24,10), new SKPoint(25,11), new SKPoint(25,10), new SKPoint(26,11), new SKPoint(26,10)}, // 100
            new SKPoint[] {new SKPoint(24,13), new SKPoint(24,12), new SKPoint(25,13), new SKPoint(25,12), new SKPoint(26,13), new SKPoint(26,12)}, // 101
            new SKPoint[] {new SKPoint(24,15), new SKPoint(24,14), new SKPoint(25,15), new SKPoint(25,14), new SKPoint(26,15), new SKPoint(26,14)}, // 102
            new SKPoint[] {new SKPoint(24,17), new SKPoint(24,16), new SKPoint(25,17), new SKPoint(25,16), new SKPoint(26,17), new SKPoint(26,16)}, // 103
            new SKPoint[] {new SKPoint(24,19), new SKPoint(24,18), new SKPoint(25,19), new SKPoint(25,18), new SKPoint(26,19), new SKPoint(26,18)}, // 104
            new SKPoint[] {new SKPoint(24,21), new SKPoint(24,20), new SKPoint(25,21), new SKPoint(25,20), new SKPoint(26,21), new SKPoint(26,20)}, // 105
            new SKPoint[] {new SKPoint(24,23), new SKPoint(24,22), new SKPoint(25,23), new SKPoint(25,22), new SKPoint(26,23), new SKPoint(26,22)}, // 106
            new SKPoint[] {new SKPoint(24,25), new SKPoint(24,24), new SKPoint(25,25), new SKPoint(25,24), new SKPoint(26,25), new SKPoint(26,24)}, // 107
            new SKPoint[] {new SKPoint(24,27), new SKPoint(24,26), new SKPoint(25,27), new SKPoint(25,26), new SKPoint(26,27), new SKPoint(26,26)}, // 108
            new SKPoint[] {new SKPoint(27,27), new SKPoint(27,26), new SKPoint(28,27), new SKPoint(28,26), new SKPoint(29,27), new SKPoint(29,26)}, // 109
            new SKPoint[] {new SKPoint(27,25), new SKPoint(27,24), new SKPoint(28,25), new SKPoint(28,24), new SKPoint(29,25), new SKPoint(29,24)}, // 110
            new SKPoint[] {new SKPoint(27,23), new SKPoint(27,22), new SKPoint(28,23), new SKPoint(28,22), new SKPoint(29,23), new SKPoint(29,22)}, // 111
            new SKPoint[] {new SKPoint(27,21), new SKPoint(27,20), new SKPoint(28,21), new SKPoint(28,20), new SKPoint(29,21), new SKPoint(29,20)}, // 112
            new SKPoint[] {new SKPoint(27,19), new SKPoint(27,18), new SKPoint(28,19), new SKPoint(28,18), new SKPoint(29,19), new SKPoint(29,18)}, // 113
            new SKPoint[] {new SKPoint(27,17), new SKPoint(27,16), new SKPoint(28,17), new SKPoint(28,16), new SKPoint(29,17), new SKPoint(29,16)}, // 114
            new SKPoint[] {new SKPoint(27,15), new SKPoint(27,14), new SKPoint(28,15), new SKPoint(28,14), new SKPoint(29,15), new SKPoint(29,14)}, // 115
            new SKPoint[] {new SKPoint(27,13), new SKPoint(27,12), new SKPoint(28,13), new SKPoint(28,12), new SKPoint(29,13), new SKPoint(29,12)}, // 116
            new SKPoint[] {new SKPoint(27,11), new SKPoint(27,10), new SKPoint(28,11), new SKPoint(28,10), new SKPoint(29,11), new SKPoint(29,10)}, // 117
            new SKPoint[] {new SKPoint(27,09), new SKPoint(27,08), new SKPoint(28,09), new SKPoint(28,08), new SKPoint(29,09), new SKPoint(29,08)}, // 118
            new SKPoint[] {new SKPoint(27,07), new SKPoint(27,06), new SKPoint(28,07), new SKPoint(28,06), new SKPoint(29,07), new SKPoint(29,06)}, // 119
            new SKPoint[] {new SKPoint(27,05), new SKPoint(27,04), new SKPoint(28,05), new SKPoint(28,04), new SKPoint(29,05), new SKPoint(29,04)}, // 120
            new SKPoint[] {new SKPoint(27,03), new SKPoint(27,02), new SKPoint(28,03), new SKPoint(28,02), new SKPoint(29,03), new SKPoint(29,02)}, // 121
            new SKPoint[] {new SKPoint(27,01), new SKPoint(27,00), new SKPoint(28,01), new SKPoint(28,00), new SKPoint(29,01), new SKPoint(29,00)}, // 122
            new SKPoint[] {new SKPoint(30,01), new SKPoint(30,00), new SKPoint(31,01), new SKPoint(31,00), new SKPoint(32,01), new SKPoint(32,00)}, // 123
            new SKPoint[] {new SKPoint(30,03), new SKPoint(30,02), new SKPoint(31,03), new SKPoint(31,02), new SKPoint(32,03), new SKPoint(32,02)}, // 124
            new SKPoint[] {new SKPoint(30,05), new SKPoint(30,04), new SKPoint(31,05), new SKPoint(31,04), new SKPoint(32,05), new SKPoint(32,04)}, // 125
            new SKPoint[] {new SKPoint(30,07), new SKPoint(30,06), new SKPoint(31,07), new SKPoint(31,06), new SKPoint(32,07), new SKPoint(32,06)}, // 126
            new SKPoint[] {new SKPoint(30,09), new SKPoint(30,08), new SKPoint(31,09), new SKPoint(31,08), new SKPoint(32,09), new SKPoint(32,08)}, // 127
            new SKPoint[] {new SKPoint(30,11), new SKPoint(30,10), new SKPoint(31,11), new SKPoint(31,10), new SKPoint(32,11), new SKPoint(32,10)}, // 128
            new SKPoint[] {new SKPoint(30,13), new SKPoint(30,12), new SKPoint(31,13), new SKPoint(31,12), new SKPoint(32,13), new SKPoint(32,12)}, // 129
            new SKPoint[] {new SKPoint(30,15), new SKPoint(30,14), new SKPoint(31,15), new SKPoint(31,14), new SKPoint(32,15), new SKPoint(32,14)}, // 130
            new SKPoint[] {new SKPoint(30,17), new SKPoint(30,16), new SKPoint(31,17), new SKPoint(31,16), new SKPoint(32,17), new SKPoint(32,16)}, // 131
            new SKPoint[] {new SKPoint(30,19), new SKPoint(30,18), new SKPoint(31,19), new SKPoint(31,18), new SKPoint(32,19), new SKPoint(32,18)}, // 132
            new SKPoint[] {new SKPoint(30,21), new SKPoint(30,20), new SKPoint(31,21), new SKPoint(31,20), new SKPoint(32,21), new SKPoint(32,20)}, // 133
            new SKPoint[] {new SKPoint(30,23), new SKPoint(30,22), new SKPoint(31,23), new SKPoint(31,22), new SKPoint(32,23), new SKPoint(32,22)}, // 134
            new SKPoint[] {new SKPoint(30,25), new SKPoint(30,24), new SKPoint(31,25), new SKPoint(31,24), new SKPoint(32,25), new SKPoint(32,24)}, // 135
            new SKPoint[] {new SKPoint(30,27), new SKPoint(30,26), new SKPoint(31,27), new SKPoint(31,26), new SKPoint(32,27), new SKPoint(32,26)}, // 136
            new SKPoint[] {new SKPoint(01,28), new SKPoint(02,29), new SKPoint(02,28), new SKPoint(03,28), new SKPoint(04,29), new SKPoint(04,28)}, // 137
            new SKPoint[] {new SKPoint(05,28), new SKPoint(06,29), new SKPoint(06,28), new SKPoint(07,28), new SKPoint(08,29), new SKPoint(08,28)}, // 138
            new SKPoint[] {new SKPoint(09,28), new SKPoint(10,29), new SKPoint(10,28), new SKPoint(11,28), new SKPoint(12,29), new SKPoint(12,28)}, // 139
            new SKPoint[] {new SKPoint(13,28), new SKPoint(14,29), new SKPoint(14,28), new SKPoint(15,28), new SKPoint(16,29), new SKPoint(16,28)}, // 140
            new SKPoint[] {new SKPoint(17,28), new SKPoint(18,29), new SKPoint(18,28), new SKPoint(19,28), new SKPoint(20,29), new SKPoint(20,28)}, // 141
            new SKPoint[] {new SKPoint(21,28), new SKPoint(22,29), new SKPoint(22,28), new SKPoint(23,28), new SKPoint(24,29), new SKPoint(24,28)}, // 142
            new SKPoint[] {new SKPoint(25,28), new SKPoint(26,29), new SKPoint(26,28), new SKPoint(27,28), new SKPoint(28,29), new SKPoint(28,28)}, // 143
            new SKPoint[] {new SKPoint(29,28), new SKPoint(30,29), new SKPoint(30,28), new SKPoint(31,28), new SKPoint(32,29), new SKPoint(32,28)}, // 144
        };

        private enum codeSets
        {
            setA,
            setB,
            setC,
            setD,
            setE,
        }

        private static char[] c_setA = 
        {
            '\r','A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
            'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '\0', '\x1C', '\x1D',
            '\x1E', '\0', ' ', '\0', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+',
            ',',  '—', '.', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':',
        };

        private static char[] c_setB = 
        {
            '`','a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
            'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '\0', '\x1C', '\x1D',
            '\x1E', '\0', '{', '\0', '}', '~', '\x7F', ';', '<', '=', '>', '?', '[', '\\',
            ']',  '^', '_', ' ', ',', '.', '/', ':', '@', '!', '|',
        };

        private static char[] c_setC = 
        {
            'À','Á', 'Â', 'Ã', 'Ä', 'Å', 'Æ', 'Ç', 'È', 'É', 'Ê', 'Ë', 'Ì', 'Í', 'Î', 'Ï',
            'Ð', 'Ñ', 'Ò', 'Ó', 'Ô', 'Õ', 'Ö', '×', 'Ø', 'Ù', 'Ú', '\0', '\x1C', '\x1D',
            '\x1E', '\0', 'Û', 'Ü', 'Ý', 'Þ', 'ß', 'ª', '¬', '±', '²', '³', 'µ', '¹',
            'º',  '¼', '½', '¾', '\x80', '\x81', '\x82', '\x83', '\x84', '\x85', '\x86',
            '\x87', '\x88', '\x89', '\0', ' ',
        };//

        private static char[] c_setD = 
        {
            'à','á', 'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 'î', 'ï',
            'ð', 'ñ', 'ò', 'ó', 'ô', 'õ', 'ö', '÷', 'ø', 'ù', 'ú', '\0', '\x1C', '\x1D',
            '\x1E', '\0', 'û', 'ü', 'ý', 'þ', 'ÿ', '¡', '¨', '«', '¯', '°', '´', '·',
            '¸',  '»', '¿', '\x8A', '\x8B', '\x8C', '\x8D', '\x8E', '\x8F', '\x90',
            '\x91', '\x92', '\x93', '\x94', '\0', ' ',
        };//


        private static char[] c_setE = 
        {
            '\0','\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\a', '\b', '\t', '\n',
            '\v', '\f', '\r', '\x0E', '\x0F', '\x10', '\x11', '\x12', '\x13', '\x14',
            '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A','\0','\0','\0', '\x1B', '\0',
            '\x1C', '\x1D', '\x1E', '\x1F', '\x9F', '\xA0', '¢', '£', '¤', '¥', '¦', '§',
            '©', '\xAD', '®',  '¶', '\x95', '\x96', '\x97', '\x98', '\x99', '\x9A',
            '\x9B', '\x9C', '\x9D', '\x9E', '\0', ' ',
        };

        // Control Characters
        private static byte NS = 31; // Numeric Shift
        private static byte Shift2A = 56; // 2 Shift A
        private static byte Shift3A = 57; // 3 Shift A
        private static byte LatchA = 58; // Latch A
        private static byte ShiftAB = 59; // Shift A & Shift B
        private static byte LatchAB = 63; // Latch A & Latch B
        private static byte ShiftC = 60; // Shift C & Lock-In-C
        private static byte ShiftD = 61; // Shift D & Lock-In-D
        private static byte ShiftE = 62; // Shift E & Lock-In-E
        private static byte Pad = 33; // Padding

        private ArrayList hexagonsList;
        private int m_symbolWidth = 300; // in pixels
        private int m_realSymbolWidth = 300; // in pixels
        private int m_symbolHeight = 300; // in pixels
        private int m_realSymbolHeight = 300; // in pixels

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxiCodeSymbology"/> class.
        /// </summary>
        public MaxiCodeSymbology()
            : base(TrueSymbologyType.MaxiCode)
        {
        }

        public MaxiCodeSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.MaxiCode)
        {
        }

        /// <summary>
        /// Validates the value using MaxiCode symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            try
            {
                byte[] bytes = null;
                switch (Options.MaxiCodeMode)
                {
                    case 2:
                    case 3:
                    {
                        bytes = getCodewordsSequence23(value);
                        break;
                    }
                    case 4:
                    case 5:
                    case 6:
                    {
                        bytes = getCodewordsSequence(value);
                        break;
                    }
                }

                getSymbolSigns(bytes);
            }
            catch (Exception)
            {
               return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "MaxiCode symbology allows a maximum data size of 138 numeric or 93 alphabetic characters.\n";
        }


        /// <summary>
        /// Gets the barcode value encoded using MaxiCode symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using MaxiCode symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return "";
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            return "";
        }

        private struct SignCode
        {
            byte signvalue;
            codeSets codeset;

            public SignCode(int v, codeSets code)
            {
                signvalue = (byte)v;
                codeset = code;
            }

            public byte SignValue
            {
                get { return signvalue; }
                set { signvalue = value; }
            }

            public codeSets CodeSet
            {
                get { return codeset; }
                set { codeset = value; }
            }
        }

        private sbyte getSignValue(char sign, char[] set)
        {
            for (sbyte i = 0; i < set.Length; i++)
            {
                if (sign == set[i])
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets the sign value and code set.
        /// </summary>
        /// <param name="sign">The sign.</param>
        /// <param name="current">The current code set.</param>
        /// <returns></returns>
        private SignCode getSignValueCodeSets(char sign, codeSets current)
        {
            if (sign == '\0')
            {
                return new SignCode(0, codeSets.setE);
            }

            sbyte index = -1;
            // because few (five?) punctuation signs setA and setB are the same,
            // then we try to optimize
            if (current == codeSets.setB)
            {
                index = getSignValue(sign, c_setB);
                if (index > -1)
                {
                    return new SignCode(index, codeSets.setB);
                }

                index = getSignValue(sign, c_setA);
                if (index > -1)
                {
                    return new SignCode(index, codeSets.setA);
                }
            }
            else
            {
                index = getSignValue(sign, c_setA);
                if (index > -1)
                {
                    return new SignCode(index, codeSets.setA);
                }

                index = getSignValue(sign, c_setB);
                if (index > -1)
                {
                    return new SignCode(index, codeSets.setB);
                }
            }

            index = getSignValue(sign, c_setC);
            if (index > -1)
            {
                return new SignCode(index, codeSets.setC);
            }

            index = getSignValue(sign, c_setD);
            if (index > -1)
            {
                return new SignCode(index, codeSets.setD);
            }

            index = getSignValue(sign, c_setE);
            if (index > -1)
            {
                return new SignCode(index, codeSets.setE);
            }

            return new SignCode(-1, codeSets.setA);
        }

        /// <summary>
        /// Gets the codewords sequence for 4, 5, and 6 modes.
        /// </summary>
        /// <param name="value">The message for encoding.</param>
        /// <returns></returns>
        private byte[] getCodewordsSequence(string value)
        {
            byte[] charBytes = Options.Encoding.GetBytes(value);

            string message = Encoding.ASCII.GetString(charBytes, 0, charBytes.Length);

            byteList bytes = new byteList();
            if (Options.MaxiCodeMode != 2 && Options.MaxiCodeMode != 3)
                bytes.Add(Options.MaxiCodeMode);

            codeSets currentSet = codeSets.setA;
            SignCode sign1 = new SignCode(0, currentSet);
            SignCode sign2 = new SignCode(0, currentSet);
            SignCode sign3 = new SignCode(0, currentSet);
            SignCode sign4 = new SignCode(0, currentSet);
            int i = 0;
            while (i < message.Length)
            {
                // Numeric Shift
                if ((char.IsDigit(message[i])) && (i + 9 < message.Length))
                {
                    bool ns = true;
                    for (int j = 1; j < 9; j++)
                    {
                        if (!char.IsDigit(message[i + j]))
                        {
                            ns = false;
                            break;
                        }
                    }
                    if (ns)
                    {
                        // use numeric shift
                        int number = int.Parse(message.Substring(i, 9));
                        String bin_number = Convert.ToString(number, 2);
                        while (bin_number.Length < 30)
                            bin_number = bin_number.Insert(0, "0");
                        bytes.Add(NS);
                        for (int j = 0; j < 30; j += 6)
                            bytes.Add(Convert.ToByte(bin_number.Substring(j, 6), 2));
                        i += 9;
                        break;
                    }
                }

                sign1 = getSignValueCodeSets(message[i], currentSet);
                if (i + 1 < message.Length)
                {
                    sign2 = getSignValueCodeSets(message[i + 1], currentSet);
                    if (i + 2 < message.Length)
                    {
                        sign3 = getSignValueCodeSets(message[i + 2], currentSet);
                        if (i + 3 < message.Length)
                            sign4 = getSignValueCodeSets(message[i + 3], currentSet);
                    }
                }

                if (currentSet == sign1.CodeSet)
                    bytes.Add(sign1.SignValue);

                else
                    switch (sign1.CodeSet)
                    {
                        case codeSets.setA:
                            if (currentSet == codeSets.setB)
                            {
                                if (sign2.CodeSet == codeSets.setA &&
                                    sign3.CodeSet == codeSets.setA &&
                                    sign4.CodeSet == codeSets.setA &&
                                    (i + 3 < message.Length))
                                {
                                    // Latch A and switching into codeSets.setA
                                    bytes.Add(LatchAB);
                                    bytes.Add(sign1.SignValue);
                                    currentSet = codeSets.setA;
                                }
                                else if (sign2.CodeSet == codeSets.setA &&
                                         sign3.CodeSet == codeSets.setA &&
                                         (i + 2 < message.Length))
                                {
                                    // 3 Shift A and writing 3 characters into codeSets.setA                                    
                                    // but what if the next characters will not be codeSets.setB?
                                    bytes.Add(Shift3A);
                                    bytes.Add(sign1.SignValue);
                                    bytes.Add(sign2.SignValue);
                                    bytes.Add(sign3.SignValue);
                                    i += 2; // also adding i++ at the end of the block
                                }
                                else if (sign2.CodeSet == codeSets.setA && (i + 1 < message.Length))
                                {
                                    // 2 Shift A and writing 2 characters into codeSets.setA
                                    bytes.Add(Shift2A);
                                    bytes.Add(sign1.SignValue);
                                    bytes.Add(sign2.SignValue);
                                    i += 1; // and i++ in the end of the block
                                }
                                else
                                {
                                    // Shift A and writing one character into codeSets.setA
                                    bytes.Add(ShiftAB);
                                    bytes.Add(sign1.SignValue);
                                }
                            }
                            else
                            {
                                // Latch A and switching into codeSets.setA
                                bytes.Add(LatchA);
                                bytes.Add(sign1.SignValue);
                                currentSet = codeSets.setA;
                            }
                            break;
                        case codeSets.setB:
                            if (currentSet == codeSets.setA)
                            {
                                if ((sign2.CodeSet == codeSets.setB) && (i + 1 < message.Length))
                                {
                                    // Latch B and switching into codeSets.setB
                                    bytes.Add(LatchAB);
                                    bytes.Add(sign1.SignValue);
                                    currentSet = codeSets.setB;
                                }
                                else
                                {
                                    // Shift B and writing one character into codeSets.setB
                                    bytes.Add(ShiftAB);
                                    bytes.Add(sign1.SignValue);
                                }
                            }
                            else
                            {
                                // Latch B and switching into codeSets.setB
                                bytes.Add(LatchAB);
                                bytes.Add(sign1.SignValue);
                                currentSet = codeSets.setB;
                            }
                            break;
                        case codeSets.setC:
                            if ((sign2.CodeSet == codeSets.setC) && (i + 1 < message.Length))
                            {
                                // Lock-In C and switching into codeSets.setC
                                bytes.Add(ShiftC);
                                bytes.Add(ShiftC);
                                bytes.Add(sign1.SignValue);
                                currentSet = codeSets.setC;
                            }
                            else
                            {
                                // Shift C and writing one character into codeSets.setC
                                bytes.Add(ShiftC);
                                bytes.Add(sign1.SignValue);
                            }
                            break;
                        case codeSets.setD:
                            if ((sign2.CodeSet == codeSets.setD) && (i + 1 < message.Length))
                            {
                                // Lock-In D and switching into codeSets.setD
                                bytes.Add(ShiftD);
                                bytes.Add(ShiftD);
                                bytes.Add(sign1.SignValue);
                                currentSet = codeSets.setD;
                            }
                            else
                            {                               
                                // Shift D and writing one character into codeSets.setD
                                bytes.Add(ShiftD);
                                bytes.Add(sign1.SignValue);
                            }
                            break;
                        case codeSets.setE:
                            if ((sign2.CodeSet == codeSets.setE) && (i + 1 < message.Length))
                            {
                                // Lock-In E and switching into codeSets.setE
                                bytes.Add(ShiftE);
                                bytes.Add(ShiftE);
                                bytes.Add(sign1.SignValue);
                                currentSet = codeSets.setE;
                            }
                            else
                            {
                                // Shift E and writing one character into codeSets.setE
                                bytes.Add(ShiftE);
                                bytes.Add(sign1.SignValue);
                            }
                            break;
                    }
                i++;
            }

            // depending on Options.MaxiCodeMode we fill with paddings
            if (Options.MaxiCodeMode == 5)
            {
                if (bytes.Count > first_message_length + second_message_length - eec_length)
                    throw new BarcodeException("String is too long");
                else
                    while (bytes.Count < first_message_length + second_message_length - eec_length)
                        bytes.Add(Pad);
            }
            else if (Options.MaxiCodeMode == 2 || Options.MaxiCodeMode == 3)
            {
                if (bytes.Count > second_message_length - sec_length)
                    throw new BarcodeException("String is too long");
                else
                    while (bytes.Count < second_message_length - sec_length)
                        bytes.Add(Pad);
            }
            else
            {
                if (bytes.Count > first_message_length + second_message_length - sec_length)
                    throw new BarcodeException("String is too long");
                else
                    while (bytes.Count < first_message_length + second_message_length - sec_length)
                        bytes.Add(Pad);
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// Gets the codewords sequence for 2, and 3 modes.
        /// </summary>
        /// <param name="message">The message for encoding.</param>
        /// <returns></returns>
        private byte[] getCodewordsSequence23(string message)
        {
            try
            {
                StringBuilder binary_str = new StringBuilder();
                string ups_sig = "[)>" + '\x1E' + "01" + '\x1D';
                bool ups = false;
                int index1 = 0;
                if (ups_sig == message.Substring(0, 7))
                {
                    ups = true;
                    index1 = 9;
                }
                int index2 = message.IndexOf('\x1D', index1);
                string postalcode = message.Substring(index1, index2 - index1);

                index1 = index2 + 1;
                index2 = message.IndexOf('\x1D', index1);
                string countrycode = message.Substring(index1, index2 - index1);
                string countrycode_bin = Convert.ToString(int.Parse(countrycode), 2);
                if (countrycode_bin.Length > 10)
                    throw new BarcodeException("Ivalid MaxiCode mode");
                while (countrycode_bin.Length < 10)
                    countrycode_bin = countrycode_bin.Insert(0, "0");

                index1 = index2 + 1;
                index2 = message.IndexOf('\x1D', index1);
                string CoS = message.Substring(index1, index2 - index1);
                string CoS_bin = Convert.ToString(int.Parse(CoS), 2);
                if (CoS_bin.Length > 10)
                    throw new BarcodeException("Ivalid MaxiCode mode");
                while (CoS_bin.Length < 10)
                    CoS_bin = CoS_bin.Insert(0, "0");

                if (ups)
                {
                    // must be set mode 2, if postal code is numeric,
                    // and mode 3, if postal code is alphanumeric
                    Options._maxiCodeMode = 2;
                    for (int i = 0; i < postalcode.Length; i++)
                        if (!char.IsDigit(postalcode[i]))
                        {
                            Options._maxiCodeMode = 3;
                            break;
                        }
                }
                else if (Options.MaxiCodeMode == 2)
                {
                    for (int i = 0; i < postalcode.Length; i++)
                        if (!char.IsDigit(postalcode[i]))
                            throw new BarcodeException("Ivalid MaxiCode mode");
                }

                if (Options.MaxiCodeMode == 2)
                {
                    binary_str.Append("0010"); // mode 2
                    string postalcode_length = Convert.ToString(postalcode.Length, 2);
                    while (postalcode_length.Length < 6)
                        postalcode_length = postalcode_length.Insert(0, "0");
                    string postalcode_bin2 = Convert.ToString(int.Parse(postalcode), 2);
                    while (postalcode_bin2.Length < 30)
                        postalcode_bin2 = postalcode_bin2.Insert(0, "0");
                    binary_str.Insert(0, postalcode_bin2.Substring(28, 2));//  1..2
                    binary_str.Append(postalcode_bin2.Substring(22, 6));   //  7..12
                    binary_str.Append(postalcode_bin2.Substring(16, 6));   // 13..18
                    binary_str.Append(postalcode_bin2.Substring(10, 6));   // 19..24
                    binary_str.Append(postalcode_bin2.Substring(4, 6));    // 25..30
                    binary_str.Append(postalcode_length.Substring(4, 2));  // 31..32
                    binary_str.Append(postalcode_bin2.Substring(0, 4));    // 33..36
                    binary_str.Append(countrycode_bin.Substring(8, 2));    // 37..38
                    binary_str.Append(postalcode_length.Substring(0, 4));  // 39..42
                }
                else
                {
                    binary_str.Append("0011"); // mode 3, 3..6
                    if (postalcode.Length > 6)
                        postalcode = postalcode.Substring(0, 6);
                    else
                        while (postalcode.Length < 6)
                            postalcode = postalcode.Insert(0, "0");
                    string postalcode_bin3 = string.Empty;
                    for (int i = 0; i < postalcode.Length; i++)
                    {
                        SignCode sign = new SignCode(postalcode[i], codeSets.setA);
                        if (sign.CodeSet == codeSets.setA)
                        {
                            string bin = Convert.ToString(sign.SignValue, 2);
                            while (bin.Length < 6)
                                bin = bin.Insert(0, "0");
                            postalcode_bin3 += bin;
                        }
                    }

                    binary_str.Insert(0, postalcode_bin3.Substring(34, 2));//  1..2
                    binary_str.Append(postalcode_bin3.Substring(28, 6));   //  7..12
                    binary_str.Append(postalcode_bin3.Substring(22, 6));   // 13..18
                    binary_str.Append(postalcode_bin3.Substring(16, 6));   // 19..24
                    binary_str.Append(postalcode_bin3.Substring(10, 6));   // 25..30
                    binary_str.Append(postalcode_bin3.Substring(4, 6));    // 31..36
                    binary_str.Append(countrycode_bin.Substring(8, 2));    // 37..38
                    binary_str.Append(postalcode_bin3.Substring(0, 4));    // 39..42
                }
                binary_str.Append(countrycode_bin.Substring(2, 6));    // 43..48
                binary_str.Append(CoS_bin.Substring(6, 4));            // 49..52
                binary_str.Append(countrycode_bin.Substring(0, 2));    // 53..54
                binary_str.Append(CoS_bin.Substring(0, 6));            // 55..60



                // first message
                string message1 = binary_str.ToString();
                byte[] first_message = new byte[10];
                for (int i = 0; i < first_message.Length; i++)
                {
                    first_message[i] = Convert.ToByte(message1.Substring(i * 6, 6), 2);
                }

                // secondary message
                string message2;
                if (ups)
                    message2 = message.Substring(0, 9) + message.Substring(index2 + 1);
                else
                    message2 = message.Substring(index2 + 1);
                byte[] second_message = getCodewordsSequence(message2);

                byteList bytes = new byteList();
                bytes.AddRange(first_message);
                bytes.AddRange(second_message);
                return bytes.ToArray();

            }
            catch (Exception)
            {
                throw new BarcodeException("Ivalid Structured Carrier Message");
            }
        }

        private byte[] getSymbolSigns(byte[] codewords)
        {
            // checking codewords size, it must be 94 or 78
            if (Options.MaxiCodeMode == 5 && codewords.Length != first_message_length + second_message_length - eec_length)
                throw new BarcodeException("Ivalid MaxiCode mode");
            else if (Options.MaxiCodeMode != 5 && codewords.Length != first_message_length + second_message_length - sec_length)
                throw new BarcodeException("Ivalid MaxiCode mode");

            byteList bytes = new byteList();

            // initial message
            byte[] first_message = new byte[first_message_length];
            //first_message[0] = bytes[0];
            for (int i = 0; i < first_message_length; i++)
            {
                bytes.Add(codewords[i]);
                first_message[i] = codewords[i];
            }
            // calculating error correction codewords
            RSCodec rs = new RSCodec(67);
            int[] ecc = rs.Encode(first_message, first_message_length);            
            // and adding them to the end of the initial message
            for (int i = ecc.Length - 1; i >= 0; i--)
                bytes.Add((byte)ecc[i]);

            // second message
            byteList odd_subset = new byteList();
            byteList even_subset = new byteList();
            for (int i = first_message_length; i < codewords.Length; i++)
            {
                bytes.Add(codewords[i]);
                if ((i % 2) == 0)
                    even_subset.Add(codewords[i]);
                else
                    odd_subset.Add(codewords[i]);
            }
            int ecclen;
            if (Options.MaxiCodeMode == 5)
                ecclen = eec_length / 2;
            else
                ecclen = sec_length / 2;
            int[] ecc_odd = rs.Encode(odd_subset.ToArray(), ecclen);
            int[] ecc_even = rs.Encode(even_subset.ToArray(), ecclen);
            for (int i = ecc_odd.Length - 1; i >= 0; i--)
            {
                bytes.Add((byte)ecc_even[i]);
                bytes.Add((byte)ecc_odd[i]);
            }
            //#if DEBUG
            //            for (int i = 0; i < bytes.Count; i++)
            //            {
            //                System.Diagnostics.Debug.Write(bytes[i].ToString() + ",");
            //            }
            //#endif

            return bytes.ToArray();
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            //
            //              / \
            //            /     \
            //          /         \
            //        |             |
            //        |             |
            //        |             |
            //        |______w______|  t
            //        |             | 
            //        |             |   
            //          \         / |
            //            \     /   | h
            //              \ /_____|
            //
            // R=t; h=t/2

            if (Options.MaxiCodeMode > 6)
                throw new BarcodeException("Ivalid MaxiCode mode");

            SKSize drawingSize = new SKSize();
            byte[] bytes1;
            if (Options.MaxiCodeMode == 2 || Options.MaxiCodeMode == 3)
                bytes1 = getCodewordsSequence23(Value);
            else
                bytes1 = getCodewordsSequence(Value);
            byte[] codewords = getSymbolSigns(bytes1);
            if (Options.MaxiCodeEnableCustomWidth)
            {
                m_symbolWidth = NarrowBarWidth * 29;
                m_symbolHeight = m_symbolWidth;
            }
            else
            {
                // Width of symbol from the center of the leftmost module to the center of the rightmost module = 25.5mm (24-27mm)
                float dpiX = 96;
                m_symbolWidth = (int)Math.Round(dpiX);
                // height of symbol from the center of the top row to the center of the bottom row
                //m_symbolHeight = (int)Math.Round(graphics.DpiY * 0.9556); 

                // because used instead of m_symbolWidth for finding height of hexagon
                float dpiY = 96f;
                m_symbolHeight = (int)Math.Round(dpiY); 
                NarrowBarWidth = (int)Math.Round(m_symbolWidth / 29.0);
            }
            // width of hexagon
            int w = NarrowBarWidth;
            // width from center of the leftmost module to the center of the rightmost module
            m_realSymbolWidth = w * 29;

            // Height of hexagon (radius of the inscribed circle, if DpiX==DpiY)
            int R = (int)Math.Round(m_symbolHeight * 0.0398172599441121217); 
            int Y = (int)Math.Round(m_symbolHeight * 0.0298629449580840913); // Y = t + h = m_realSymbolWidth /sqrt(3)/29*3/2
            double dY;
            if (m_symbolHeight < 150)
                dY = m_symbolHeight * 0.0298629449580840913;
            else
                dY = Y;
            
            // "full" height from the top of the top row to the bottom of the bottom row
            m_realSymbolHeight = (int)Math.Round(dY * 33.3333333);
            dY = m_realSymbolHeight / 33.3333333;

            hexagonsList = new ArrayList();
            for (int i = 0; i < codewords.Length; i++)
            {
                StringBuilder sign = new StringBuilder(Convert.ToString(codewords[i], 2));
                while (sign.Length < 6)
                {
                    sign.Insert(0, "0");
                }
                // array with coordinates of hexagons for i sign of symbol
                SKPoint[] mpoins = c_symbolMap[i]; 
                for (int j = 0; j < sign.Length; j++)
                {
                    if (sign[j] == '1')
                    {
                        float x = w * mpoins[j].Y;
                        //int y = Y * mpoins[j].X;
                        float y = (int)Math.Round(dY * mpoins[j].X);
                        if (mpoins[j].X % 2 != 0)
                            x += (int)Math.Round(w * 0.5);
                        hexagonsList.Add(createHexagon(x, y, w, R)); // adding SKPoint[]
                    }
                }
            }

            for (int i = 0; i < c_orientationTemplate.Length; i++)
            {
                float x = w * c_orientationTemplate[i].Y;
                //int y = Y * c_orientationTemplate[i].X;
                float y = (float)Math.Round(dY * c_orientationTemplate[i].X);
                if (c_orientationTemplate[i].X % 2 != 0)
                    x += (int)Math.Round(w * 0.5);
                hexagonsList.Add(createHexagon(x, y, w, R)); // adding SKPoint[]
            }

            drawingSize.Width = w * 30;
            drawingSize.Height = m_realSymbolHeight;

            return drawingSize;
        }

        protected override void drawBars(SKCanvas canvas, SKPaint paint, SKPoint position)
        {
            float strokeWidth = 1f;
            SKColor strokeColor = paint.Color; // equivalent of pen using brush color

            // Draw all hexagons
            for (int i = 0; i < hexagonsList.Count; i++)
            {
                var hexPoints = (SKPoint[])hexagonsList[i];
                DrawHexagon(canvas, hexPoints, paint.Color, strokeColor, strokeWidth, position);
            }

            // Draw circles (you need a SkiaSharp version of drawCircles)
            drawCircles(canvas, paint.Color, position);
        }

        protected void DrawHexagon(SKCanvas canvas, SKPoint[] points, SKColor fillColor, SKColor strokeColor, float strokeWidth, SKPoint position)
        {
            // Offset all points by position
            SKPoint[] offsetPoints = new SKPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                offsetPoints[i] = new SKPoint(points[i].X + position.X, points[i].Y + position.Y);
            }

            // Create path for polygon
            using (var path = new SKPath())
            {
                path.MoveTo(offsetPoints[0]);
                for (int i = 1; i < offsetPoints.Length; i++)
                    path.LineTo(offsetPoints[i]);
                path.Close();

                // Fill polygon
                using (var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = fillColor,
                    IsAntialias = true
                })
                {
                    canvas.DrawPath(path, fillPaint);
                }

                // Draw polygon outline
                using (var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = strokeColor,
                    StrokeWidth = strokeWidth,
                    IsAntialias = true
                })
                {
                    canvas.DrawPath(path, strokePaint);
                }
            }
        }

        protected void drawCircles(SKCanvas canvas, SKColor color, SKPoint position)
        {
            drawCircle(canvas, color, position, 3.29, 3.98, 3.61);
            drawCircle(canvas, color, position, 5.04, 6.85, 5.81);
            drawCircle(canvas, color, position, 10.81, 25, 15.09);
            //drawCircle2(grfx, color, position, 3.29);
            //drawCircle2(grfx, Color.White, position, 3.98);
            //drawCircle2(grfx, color, position, 5.04);
            //drawCircle2(grfx, Color.White, position, 6.85);
            //drawCircle2(grfx, color, position, 10.81);
            //drawCircle2(grfx, Color.White, position, 25);
        }

        protected void drawCircle(SKCanvas canvas, SKColor color, SKPoint position, double r1, double r2, double r12)
        {
            // Calculate pen width
            float penWidth = (float)Math.Round(m_realSymbolWidth * (1 / r1 - 1 / r2) * 0.5);

            bool oddWidth = m_realSymbolWidth % 2 != 0;
            bool oddHeight = m_realSymbolHeight % 2 != 0;

            double diameter = m_realSymbolWidth / r12;
            int Dx = (int)Math.Round(diameter);
            int Dy = Dx;

            // Adjust Dx for odd/even width
            if ((oddWidth && (Dx % 2 == 0)) || (!oddWidth && (Dx % 2 != 0)))
                Dx--;

            int x = (int)((m_realSymbolWidth - Dx) * 0.5);

            // Adjust Dy for odd/even height
            if ((oddHeight && (Dy % 2 == 0)) || (!oddHeight && (Dy % 2 != 0)))
                Dy--;

            int y = (int)((m_realSymbolHeight - Dy) * 0.5);

            // Create paint for stroke
            using (var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = penWidth,
                IsAntialias = true
            })
            {
                // Draw oval
                var rect = new SKRect(
                    x + position.X,
                    y + position.Y,
                    x + position.X + Dx,
                    y + position.Y + Dy
                );

                canvas.DrawOval(rect, paint);
            }
        }

        //protected void drawCircle2(Graphics grfx, Color color, SKPoint position, double r)
        //{
        //    Brush brush = new SolidBrush(color);

        //    bool oddWidth = m_realSymbolWidth % 2 != 0;
        //    bool oddHeight = m_realSymbolHeight % 2 != 0;
        //    double diameter = m_realSymbolWidth / r;
        //    int Dx = (int)Math.Round(diameter);

        //    if (m_realSymbolWidth < 205)
        //    {
        //        Dx--;
        //    }

        //    int Dy = Dx;
        //    if ((oddWidth && (Dx % 2 == 0)) || (!oddWidth && (Dx % 2 != 0)))
        //    {
        //        Dx--;
        //    }
        //    int x = (int)((m_realSymbolWidth - Dx) * 0.5);

        //    if ((oddHeight && (Dy % 2 == 0)) || (!oddHeight && (Dy % 2 != 0)))
        //    {
        //        Dy--;
        //    }
        //    int y = (int)((m_realSymbolHeight - Dy) * 0.5);
        //    grfx.FillEllipse(brush, x + position.X, y + position.Y, Dx, Dy);
        //}
        protected SKPoint[] createHexagon(float x, float y, float w, float v)
        {
            float r = w * 0.5f; // Radius of inscribed circle
            float y1 = v * 0.25f;
            float y2 = v * 0.75f;

            SKPoint[] hex;
            if (((int)w) % 2 == 0)
                hex = new SKPoint[8];
            else
                hex = new SKPoint[6];

            hex[0] = new SKPoint(x, y + y1);

            if (hex.Length == 6)
            {
                hex[1] = new SKPoint(x + r, y);
                hex[2] = new SKPoint(x + w - 1, y + y1);
                hex[3] = new SKPoint(x + w - 1, y + y2);
                hex[4] = new SKPoint(x + r, y + v);
                hex[5] = new SKPoint(x, y + y2);
            }
            else
            {
                hex[1] = new SKPoint(x + (float)Math.Floor(r - 1), y);
                hex[2] = new SKPoint(x + (float)Math.Ceiling(r), y);
                hex[3] = new SKPoint(x + w - 1, y + y1);
                hex[4] = new SKPoint(x + w - 1, y + y2 - 1);
                hex[5] = new SKPoint(x + (float)Math.Ceiling(r), y + v - 1);
                hex[6] = new SKPoint(x + (float)Math.Floor(r - 1), y + v - 1);
                hex[7] = new SKPoint(x, y + y2 - 1);
            }

            return hex;
        }
    }
}